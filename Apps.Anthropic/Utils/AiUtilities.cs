using System.Text;
using Apps.Anthropic.Api;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using RestSharp;

namespace Apps.Anthropic.Utils;

public class AiUtilities(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
{
    public async Task<ResponseMessage> SendMessageAsync(CompletionRequest input, GlossaryRequest glossaryRequest)
    {
        var client = new AnthropicRestClient(invocationContext.AuthenticationCredentialsProviders);
        var messages = await GenerateChatMessages(input, glossaryRequest);

        var request = new RestRequest("/messages", Method.Post)
            .AddJsonBody(new
            {
                system = input.SystemPrompt ?? "",
                model = input.Model,
                messages,
                max_tokens = input.MaxTokensToSample ?? 4096,
                stop_sequences = input.StopSequences != null ? input.StopSequences : new List<string>(),
                temperature = input.Temperature != null ? float.Parse(input.Temperature) : 1.0f,
                top_p = input.TopP != null ? float.Parse(input.TopP) : 1.0f,
                top_k = input.TopK != null ? input.TopK : 1,
            });

        var response = await client.ExecuteWithErrorHandling<CompletionResponse>(request);
        return new ResponseMessage
        {
            Text = response.Content.FirstOrDefault()?.Text ?? "",
            Usage = response.Usage
        }; 
    }
    
    public async Task<string> IdentifySourceLanguageAsync(string model, string content)
    {
        var systemPrompt =
            "You are a linguist. Identify the language of the following text. Your response should be in the BCP 47 (language) or (language-country). " +
            "You respond with the language only, not other text is required.";

        var snippet = content.Length > 200 ? content.Substring(0, 300) : content;
        var userPrompt = snippet + ". The BCP 47 language code: ";

        var client = new AnthropicRestClient(invocationContext.AuthenticationCredentialsProviders);
        var messages = new List<Message>
        {
            new() { Role = "user", Content = userPrompt }
        };

        var request = new RestRequest("/messages", Method.Post)
            .AddJsonBody(new
            {
                system = systemPrompt,
                model,
                messages,
                max_tokens = 4096,
                stop_sequences = new List<string>(),
                temperature = 1.0f,
                top_p = 1.0f,
                top_k = 1
            });

        var response = await client.ExecuteWithErrorHandling<CompletionResponse>(request);
        return response.Content.FirstOrDefault()?.Text ?? "";
    }
    
    private async Task<List<Message>> GenerateChatMessages(CompletionRequest input, GlossaryRequest glossaryRequest)
    {
        var messages = new List<Message>();

        string prompt = input.Prompt;
        if (glossaryRequest.Glossary != null)
        {
            var glossaryPromptPart = await GlossaryPromptHelper.GetGlossaryPromptPart(glossaryRequest, fileManagementClient);
            prompt += glossaryPromptPart;
        }

        messages.Add(new Message { Role = "user", Content = prompt });
        return messages;
    }
}