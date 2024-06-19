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
    [Action("Create completion", Description = "Send a message")]
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
        
        var translatedUnits = await TranslateXliffDocument(input, glossaryRequest, xliffDocument);

        var xDoc = xliffDocument.UpdateTranslationUnits(translatedUnits);
        var updatedDocument = XliffDocument.FromXDocument(xDoc,
            new XliffConfig { RemoveWhitespaces = true, CopyAttributes = true, IncludeInlineTags = true });

        var fileReference = await UploadUpdatedDocument(updatedDocument, input.Xliff);
        return new ProcessXliffResponse { Xliff = fileReference };
    }
    
    [Action("Post-edit XLIFF file", Description = "Updates the targets of XLIFF 1.2 files")]
    public async Task<ProcessXliffResponse> PostEditXliff([ActionParameter] ProcessXliffRequest input,
        [ActionParameter] GlossaryRequest glossaryRequest)
    {
        var xliffDocument = await LoadAndParseXliffDocument(input.Xliff);
        if (xliffDocument.TranslationUnits.Count == 0)
        {
            return new ProcessXliffResponse { Xliff = input.Xliff };
        }
        
        var translatedUnits = await PostEditXliffDocument(input, glossaryRequest, xliffDocument);

        var xDoc = xliffDocument.UpdateTranslationUnits(translatedUnits);
        var updatedDocument = XliffDocument.FromXDocument(xDoc,
            new XliffConfig { RemoveWhitespaces = true, CopyAttributes = true, IncludeInlineTags = true });

        var fileReference = await UploadUpdatedDocument(updatedDocument, input.Xliff);
        return new ProcessXliffResponse { Xliff = fileReference };
    }
    
    [Action("Get Quality Scores for XLIFF file", Description = "Gets segment and file level quality scores for XLIFF files")]
    public async Task<ScoreXliffResponse> GetQualityScores([ActionParameter] ProcessXliffRequest input,
        [ActionParameter] GlossaryRequest glossaryRequest)
    {
        var xliffDocument = await LoadAndParseXliffDocument(input.Xliff);
        if (xliffDocument.TranslationUnits.Count == 0)
        {
            return new ScoreXliffResponse { XliffFile = input.Xliff, AverageScore = 0 };
        }
        
        double averageScore = await GetQualityScoresOfXliffDocument(input, glossaryRequest, xliffDocument);

        var fileReference = await UploadUpdatedDocument(xliffDocument, input.Xliff);
        return new ScoreXliffResponse { XliffFile = fileReference, AverageScore = averageScore };
    }

    private async Task<XliffDocument> LoadAndParseXliffDocument(FileReference inputFile)
    {
        var stream = await fileManagementClient.DownloadAsync(inputFile);
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var xliffDoc = XDocument.Load(memoryStream);
        return XliffDocument.FromXDocument(xliffDoc,
            new XliffConfig { RemoveWhitespaces = true, CopyAttributes = false, IncludeInlineTags = true });
    }
    
    private async Task<List<TranslationUnit>> TranslateXliffDocument(ProcessXliffRequest request, GlossaryRequest glossaryRequest, XliffDocument xliff)
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
    
    private async Task<List<TranslationUnit>> PostEditXliffDocument(ProcessXliffRequest request, GlossaryRequest glossaryRequest, XliffDocument xliff)
    {
        foreach (var translationUnit in xliff.TranslationUnits)
        {
            var sourceLanguage = translationUnit.SourceLanguage ?? xliff.SourceLanguage;
            var targetLanguage = translationUnit.TargetLanguage ?? xliff.TargetLanguage;
            var response = await CreateCompletion(new(request)
            {
                Prompt = request.Prompt ?? 
                    $"Your input is going to be a sentence in {sourceLanguage} as source language and their translation into {targetLanguage}. " +
                    "You need to review the target text and respond with edits of the target text as necessary. If no edits are required, respond with target text." +
                    "Your reply needs to include only the target text (updated or unmodified) in the same format as received (it's crucial, because your response will be used as a translation without any further processing)." +
                    $"Translation unit: \n" +
                    $"Source: {translationUnit.Source}; Target: {translationUnit.Target}",
                SystemPrompt = request.SystemPrompt ?? "You are a linguistic expert that should process the following texts according to the given instructions."
            }, glossaryRequest);
            
            translationUnit.Target = response.Text;
        }
        
        return xliff.TranslationUnits;
    }
    
    private async Task<double> GetQualityScoresOfXliffDocument(ProcessXliffRequest request, GlossaryRequest glossaryRequest, XliffDocument xliff)
    {
        var criteria = request.Prompt ?? "fluency, grammar, terminology, style, and punctuation";
        double totalScore = 0;
        
        foreach (var translationUnit in xliff.TranslationUnits)
        {
            var sourceLanguage = translationUnit.SourceLanguage ?? xliff.SourceLanguage;
            var targetLanguage = translationUnit.TargetLanguage ?? xliff.TargetLanguage;
            var response = await CreateCompletion(new(request)
            {
                Prompt = $"Your input is going to be a sentence in {sourceLanguage} as source language and their translation into {targetLanguage}. " +
                         "You need to review the target text and respond with scores for the target text. " +
                         $"The score number is a score from 1 to 10 assessing the quality of the translation, considering the following criteria: {criteria}." +
                         $"Provide only number as response, it's crucial, because your response will be displayed to the user without any further processing" +
                         $"Translation unit: \n" +
                         $"Source: {translationUnit.Source}; Target: {translationUnit.Target}",
                SystemPrompt = request.SystemPrompt ?? "You are a linguistic expert that should process the following texts according to the given instructions."
            }, glossaryRequest);
            
            translationUnit.Attributes?.Add("extradata", response.Text);
            if (double.TryParse(response.Text, out double score))
            {
                totalScore += score;
            }
            else
            {
                throw new Exception($"Received invalid score from API. Score: {response.Text}");
            }
        }
        
        return totalScore / xliff.TranslationUnits.Count;
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