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
using Newtonsoft.Json;
using MoreLinq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;


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
        [ActionParameter] GlossaryRequest glossaryRequest, [ActionParameter,
         Display("Bucket size",
             Description =
                 "Specify the number of source texts to be translated at once. Default value: 1500. (See our documentation for an explanation)")]
        int? bucketSize = 1500)
    {
        var xliffDocument = await LoadAndParseXliffDocument(input.Xliff);
        if (xliffDocument.TranslationUnits.Count == 0)
        {
            return new ProcessXliffResponse { Xliff = input.Xliff };
        }
        var translatedUnits = await TranslateXliffDocument(input, glossaryRequest, xliffDocument, bucketSize ?? 1500);
        var stream = await fileManagementClient.DownloadAsync(input.Xliff);
        var updatedFile = Blackbird.Xliff.Utils.Utils.XliffExtensions.UpdateOriginalFile(stream, translatedUnits);
        string contentType = input.Xliff.ContentType ?? "application/xml";
        var fileReference = await fileManagementClient.UploadAsync(updatedFile, contentType, input.Xliff.Name);
        return new ProcessXliffResponse { Xliff = fileReference };
    }
    
    [Action("Post-edit XLIFF file", Description = "Updates the targets of XLIFF 1.2 files")]
    public async Task<ProcessXliffResponse> PostEditXliff([ActionParameter] ProcessXliffRequest input,
        [ActionParameter] GlossaryRequest glossaryRequest,[ActionParameter,
         Display("Bucket size",
             Description =
                 "Specify the number of translation units processed at once. Default value: 1500. (See our documentation for an explanation)")]
        int? bucketSize = 1500)
    {
        var xliffDocument = await LoadAndParseXliffDocument(input.Xliff);
        if (xliffDocument.TranslationUnits.Count == 0)
        {
            return new ProcessXliffResponse { Xliff = input.Xliff };
        }
        var translatedUnits = await PostEditXliffDocument(input, glossaryRequest, xliffDocument, bucketSize ?? 1500);
        var stream = await fileManagementClient.DownloadAsync(input.Xliff);
        var updatedFile = Blackbird.Xliff.Utils.Utils.XliffExtensions.UpdateOriginalFile(stream, translatedUnits);
        string contentType = input.Xliff.ContentType ?? "application/xml";
        var fileReference = await fileManagementClient.UploadAsync(updatedFile, contentType, input.Xliff.Name);
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
        return Blackbird.Xliff.Utils.Utils.XliffExtensions.ParseXLIFF(stream);
    }
    
    private async Task<Dictionary<string,string>> TranslateXliffDocument(ProcessXliffRequest request, GlossaryRequest glossaryRequest, XliffDocument xliff, int bucketSize)
    {
        var results = new List<string>();
        var batches = xliff.TranslationUnits.Batch(bucketSize);
        foreach (var batch in batches)
        {
            string json = JsonConvert.SerializeObject(batch.Select(x => "{ID:" + x.Id + "}" + x.Source ));
            var UserPrompt = GetUserPrompt(request.Prompt,xliff,json);
            var response = await CreateCompletion(new(request)
            {
                Prompt = UserPrompt,
                SystemPrompt = request.SystemPrompt ?? "You are tasked with localizing the provided text. Consider cultural nuances, idiomatic expressions, " +
                                "and locale-specific references to make the text feel natural in the target language. " +
                                "Reply only with the serialized JSON array of translated strings without additional formatting. Ensure the structure of the original text is preserved."
            }, glossaryRequest);

            var result = JsonConvert.DeserializeObject<string[]>(response.Text.Substring(response.Text.IndexOf("[")));
                   
            if (result.Length != xliff.TranslationUnits.Count)
            {
                throw new InvalidOperationException(
                    "Anthropic returned inappropriate response. " +
                    "The number of translated texts does not match the number of source texts. " +
                    "Probably there is a duplication or a missing text in translation unit. " +
                    "Try change model or bucket size (to lower values) or add retries to this action.");
            }

            results.AddRange(result);
                       
        }
        
        return results.ToDictionary(x => Regex.Match(x, "\\{ID:(.*?)\\}(.+)$").Groups[1].Value, y => Regex.Match(y, "\\{ID:(.*?)\\}(.+)$").Groups[2].Value);
    }

    private string GetUserPrompt(string prompt, XliffDocument xliffDocument, string json)
    {
        string instruction = string.IsNullOrEmpty(prompt)
            ? $"Translate the following texts from {xliffDocument.SourceLanguage} to {xliffDocument.TargetLanguage}."
            : $"Process the following texts as per the custom instructions: {prompt}. The source language is {xliffDocument.SourceLanguage} and the target language is {xliffDocument.TargetLanguage}. This information might be useful for the custom instructions.";

        return
            $"Please provide a translation for each individual text, even if similar texts have been provided more than once. " +
            $"{instruction} Return the outputs as a serialized JSON array of strings without additional formatting " +
            $"(it is crucial because your response will be deserialized programmatically. Please ensure that your response is formatted correctly to avoid any deserialization issues). " +
            $"Original texts (in serialized array format): {json}";
    }
    

    private async Task<Dictionary<string, string>> PostEditXliffDocument(ProcessXliffRequest request, GlossaryRequest glossaryRequest, XliffDocument xliff, int bucketSize)
    {
        var results = new Dictionary<string, string>();
        var batches = xliff.TranslationUnits.Batch(bucketSize);
        foreach (var batch in batches)
        {
            var response = await CreateCompletion(new(request)
            {
                Prompt = 
                $"Your input consists of sentences in {xliff.SourceLanguage} language with their translations into {xliff.TargetLanguage}. " +
                "Review and edit the translated target text as necessary to ensure it is a correct and accurate translation of the source text. " +
                "The XML tags in the source need to be included in the target text, don't delete or modify them. " +
                "Include only the target texts (including the necessary linguitic updates) in the format [ID:X]{target}. " +
                $"Example: [ID:1]{{target1}},[ID:2]{{target2}}. " +
                $"{request.Prompt ?? ""} Sentences: \n" +
                string.Join("\n", batch.Select(tu => $"ID: {tu.Id}; Source: {tu.Source}; Target: {tu.Target}")),
                SystemPrompt = request.SystemPrompt ?? "You are a linguistic expert that should process the following texts according to the given instructions."
            }, glossaryRequest);

            var matches = Regex.Matches(response.Text.Replace("</ept}", "</ept>}"), @"\[ID:(.+?)\]\{([\s\S]+?)\}(?=,\[|$|,?\n)").Cast<Match>().ToList();
            foreach (var match in matches)
            {
                if (match.Groups[2].Value.Contains("[ID:"))
                    continue;
                else
                    results.Add(match.Groups[1].Value, match.Groups[2].Value);
            }
        }
        
        return results;
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
            var glossaryPromptPart = await GetGlossaryPromptPart(glossaryRequest);
            prompt += glossaryPromptPart;
        }

        messages.Add(new Message { Role = "user", Content = prompt });
        return messages;
    }

    private async Task<string> GetGlossaryPromptPart(GlossaryRequest input)
    {
        var glossaryStream = await fileManagementClient.DownloadAsync(input.Glossary);
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