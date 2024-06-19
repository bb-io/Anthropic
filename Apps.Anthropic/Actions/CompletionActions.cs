using System.Text;
using System.Xml.Linq;
using Apps.Anthropic.Api;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using Blackbird.Xliff.Utils;
using Blackbird.Xliff.Utils.Models;
using RestSharp;

namespace Apps.Anthropic.Actions;

[ActionList]
public class CompletionActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseInvocable(invocationContext)
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

    [Action("Process XLIFF", Description = "Process XLIFF file, by default translating it to a target language")]
    public async Task<ProcessXliffResponse> ProcessXliff([ActionParameter] ProcessXliffRequest input,
        [ActionParameter] GlossaryRequest glossaryRequest)
    {
        var xliffDocument = await LoadAndParseXliffDocument(input.Xliff);
        if (xliffDocument.TranslationUnits.Count == 0)
        {
            return new ProcessXliffResponse { Xliff = input.Xliff };
        }
        
        var translatedUnits = await GetTranslations(input, glossaryRequest, xliffDocument);

        var xDoc = xliffDocument.UpdateTranslationUnits(translatedUnits);
        var updatedDocument = XliffDocument.FromXDocument(xDoc,
            new XliffConfig { RemoveWhitespaces = true, CopyAttributes = true, IncludeInlineTags = true });

        var fileReference = await UploadUpdatedDocument(updatedDocument, input.Xliff);
        return new ProcessXliffResponse { Xliff = fileReference };
    }

    private async Task<XliffDocument> LoadAndParseXliffDocument(FileReference inputFile)
    {
        var stream = await fileManagementClient.DownloadAsync(inputFile);
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var xliffDoc = XDocument.Load(memoryStream);
        return XliffDocument.FromXDocument(xliffDoc,
            new XliffConfig { RemoveWhitespaces = true, CopyAttributes = true, IncludeInlineTags = true });
    }
    
    private async Task<List<TranslationUnit>> GetTranslations(ProcessXliffRequest request, GlossaryRequest glossaryRequest, XliffDocument xliff)
    {
        foreach (var translationUnit in xliff.TranslationUnits)
        {
            string targetLanguage = translationUnit.TargetLanguage ?? xliff.TargetLanguage;
            var response = await CreateCompletion(new(request)
            {
                Prompt = request.Prompt ?? $"Translate the following text to {targetLanguage}: {translationUnit.Source}",
                SystemPrompt = request.SystemPrompt ?? "You are tasked with localizing the provided text. Consider cultural nuances, idiomatic expressions, " +
                                "and locale-specific references to make the text feel natural in the target language. " +
                                "Ensure the structure of the original text is preserved. Respond with the localized text." +
                                "In response provide ONLY the translation of the text (it's crucial, because your response will be used as a translation " +
                                "without any further processing)."
            }, glossaryRequest);
            
            translationUnit.Target = response.Text;
        }
        
        return xliff.TranslationUnits;
    }

    private async Task<FileReference> UploadUpdatedDocument(XliffDocument xliffDocument, FileReference originalFile)
    {
        var outputMemoryStream = xliffDocument.ToStream();

        string contentType = originalFile.ContentType ?? "application/xml";
        return await fileManagementClient.UploadAsync(outputMemoryStream, contentType, originalFile.Name);
    }

    private async Task<List<Message>> GenerateChatMessages(CompletionRequest input, GlossaryRequest glossaryRequest)
    {
        var messages = new List<Message>();

        string prompt = input.Prompt;
        if (glossaryRequest.Glossary != null)
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
        glossaryPromptPart.AppendLine(
            "Glossary entries (each entry includes terms in different languages. Each language may have a few synonymous variations which are separated by ;;):");

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