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
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Entities;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Anthropic.Actions;

[ActionList]
public class CompletionActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AnthropicInvocable(invocationContext, fileManagementClient)
{
    private readonly IFileManagementClient _fileManagementClient = fileManagementClient;

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
                Text = response.Content.FirstOrDefault()?.Text ?? "",
                Usage = response.Usage
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
        var entity = await TranslateXliffDocument(input, glossaryRequest, xliffDocument, bucketSize ?? 1500);
        var stream = await _fileManagementClient.DownloadAsync(input.Xliff);
        var updatedFile = Blackbird.Xliff.Utils.Utils.XliffExtensions.UpdateOriginalFile(stream, entity.TranslationUnits);
        string contentType = input.Xliff.ContentType ?? "application/xml";
        var fileReference = await _fileManagementClient.UploadAsync(updatedFile, contentType, input.Xliff.Name);
        return new ProcessXliffResponse { Xliff = fileReference, Usage = entity.Usage };
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
        var entity = await PostEditXliffDocument(input, glossaryRequest, xliffDocument, bucketSize ?? 1500);
        var stream = await _fileManagementClient.DownloadAsync(input.Xliff);
        var updatedFile = Blackbird.Xliff.Utils.Utils.XliffExtensions.UpdateOriginalFile(stream, entity.TranslationUnits);
        string contentType = input.Xliff.ContentType ?? "application/xml";
        var fileReference = await _fileManagementClient.UploadAsync(updatedFile, contentType, input.Xliff.Name);
        return new ProcessXliffResponse { Xliff = fileReference, Usage = entity.Usage };
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
        
        var qualityScoresEntity = await GetQualityScoresOfXliffDocument(input, glossaryRequest, xliffDocument);

        var fileReference = await UploadUpdatedDocument(xliffDocument, input.Xliff);
        return new ScoreXliffResponse { XliffFile = fileReference, AverageScore = qualityScoresEntity.Score, Usage = qualityScoresEntity.Usage };
    }
    
    private async Task<TranslateXliffDocumentEntity> TranslateXliffDocument(ProcessXliffRequest request, GlossaryRequest glossaryRequest, XliffDocument xliff, int bucketSize)
    {
        var results = new List<string>();
        var batches = xliff.TranslationUnits.Batch(bucketSize);
        
        var totalUsage = new UsageResponse();
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
            totalUsage += response.Usage;
        }

        return new(results.Where(x => !String.IsNullOrEmpty(Regex.Match(x, "\\{ID:(.*?)\\}(.+)$").Groups[1].Value))
            .ToDictionary(x => Regex.Match(x, "\\{ID:(.*?)\\}(.+)$").Groups[1].Value,
            y => Regex.Match(y, "\\{ID:(.*?)\\}(.+)$").Groups[2].Value), totalUsage);
    }
    

    private async Task<TranslateXliffDocumentEntity> PostEditXliffDocument(ProcessXliffRequest request, GlossaryRequest glossaryRequest, XliffDocument xliff, int bucketSize)
    {
        var results = new Dictionary<string, string>();
        var batches = xliff.TranslationUnits.Batch(bucketSize);

        var totalUsage = new UsageResponse();
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

            totalUsage += response.Usage;
        }

        return new(results, totalUsage);
    }
    
    private async Task<XliffQualityScoresEntity> GetQualityScoresOfXliffDocument(ProcessXliffRequest request, GlossaryRequest glossaryRequest, XliffDocument xliff)
    {
        var criteria = request.Prompt ?? "fluency, grammar, terminology, style, and punctuation";
        double totalScore = 0;

        var totalUsage = new UsageResponse();
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

            totalUsage += response.Usage;
        }
        
        return new(totalScore / xliff.TranslationUnits.Count, totalUsage);
    }

    private async Task<FileReference> UploadUpdatedDocument(XliffDocument xliffDocument, FileReference originalFile)
    {
        var outputMemoryStream = xliffDocument.ToStream();

        string contentType = originalFile.ContentType ?? "application/xml";
        return await _fileManagementClient.UploadAsync(outputMemoryStream, contentType, originalFile.Name);
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
}