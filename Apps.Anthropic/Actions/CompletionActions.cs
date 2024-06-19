using System.Text;
using Apps.Anthropic.Api;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using RestSharp;

namespace Apps.Anthropic.Actions;

[ActionList]
public class CompletionActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : BaseInvocable(invocationContext)
{
    [Action("Chat", Description = "Create a message")]
    public async Task<ResponseMessage> CreateCompletion([ActionParameter] CompletionRequest input,
        [ActionParameter] GlossaryRequest glossaryRequest)
    {
        var client = new AnthropicRestClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new RestRequest("/messages", Method.Post);
        var messages = await GenerateChatMessages(input, glossaryRequest);
        
        request.AddJsonBody(new
        {
            system = input.SystemPrompt ?? "",
            model = input.Model,
            messages = messages,
            max_tokens = input.MaxTokensToSample ?? 4096,
            stop_sequences = input.StopSequences != null ? input.StopSequences : new List<string>(),
            temperature = input.Temperature != null ? float.Parse(input.Temperature) : 1.0f,
            top_p = input.TopP != null ? float.Parse(input.TopP) : 1.0f,
            top_k = input.TopK != null ? input.TopK : 1,
        });
        
        var response = await client.ExecuteWithErrorHandling<CompletionResponse>(request);
        return new ResponseMessage
        {
            Text = response.Content.FirstOrDefault()?.Text ?? ""
        };
    }    
    
    private async Task<List<Message>> GenerateChatMessages(CompletionRequest input, GlossaryRequest? glossaryRequest)
    {
        var messages = new List<Message>();

        if (!string.IsNullOrEmpty(input.SystemPrompt))
        {
            messages.Add(new Message { Role = "system", Content = input.SystemPrompt });
        }

        string prompt = input.Prompt;
        if (glossaryRequest?.Glossary != null)
        {
            var glossaryPromptPart = await GetGlossaryPromptPart(glossaryRequest.Glossary);
            prompt += glossaryPromptPart;
        }
        
        messages.Add(new Message { Role = "user", Content = prompt });
        return messages;
    }

    private async Task<string> GetGlossaryPromptPart(FileReference glossary)
    {
        var glossaryStream = await fileManagementClient.DownloadAsync(glossary);
        var blackbirdGlossary = await glossaryStream.ConvertFromTbx();

        var glossaryPromptPart = new StringBuilder();
        glossaryPromptPart.AppendLine();
        glossaryPromptPart.AppendLine("Glossary entries (each entry includes terms in different languages. Each language may have a few synonymous variations which are separated by ;;):");

        foreach (var entry in blackbirdGlossary.ConceptEntries)
        {
            glossaryPromptPart.AppendLine();
            glossaryPromptPart.AppendLine("\tEntry:");

            foreach (var section in entry.LanguageSections)
            {
                glossaryPromptPart.AppendLine(
                    $"\t\t{section.LanguageCode}: {string.Join(";; ", section.Terms.Select(term => term.Term))}");
            }
        }

        return glossaryPromptPart.ToString();
    }
}