using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Xliff.Utils;
using Newtonsoft.Json;
using MoreLinq;
using System.Text.RegularExpressions;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Entities;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Apps.Anthropic.Utils;

namespace Apps.Anthropic.Actions;

[ActionList("Deprecated XLIFF")]
public class DeprecatedXliffActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AnthropicInvocable(invocationContext, fileManagementClient)
{
    private readonly IFileManagementClient _fileManagementClient = fileManagementClient;
    private readonly ChatActions _chatActions = new(invocationContext, fileManagementClient);

    [Action("Process XLIFF", Description = "Process XLIFF file, by default translating it to a target language")]
    public async Task<ProcessXliffResponse> ProcessXliff([ActionParameter] ProcessXliffRequest input,
        [ActionParameter] GlossaryRequest glossaryRequest, 
        [ActionParameter, Display("Bucket size", Description = "Specify the number of source texts to be translated at once. Default value: 1500. (See our documentation for an explanation)")] int? bucketSize = 1500)
    {
        return await ErrorHandler.ExecuteWithErrorHandlingAsync(async () =>
        {
            ThrowIfXliffInvalid(input.Xliff);
            
            var xliffDocument = await LoadAndParseXliffDocument(input.Xliff);
            var translationUnits = xliffDocument.Files.SelectMany(x => x.TranslationUnits).ToList();
            if (translationUnits.Count == 0)
            {
                return new ProcessXliffResponse { Xliff = input.Xliff };
            }

            var entity = await TranslateXliffDocument(input, glossaryRequest, xliffDocument, bucketSize ?? 1500);
            var updatedSegmentsCount = 0;
            foreach (var (key, value) in entity.TranslationUnits)
            {
                var translationUnit = translationUnits.FirstOrDefault(x => x.Id == key);
                if (translationUnit != null)
                {
                    if (translationUnit.Target.Content != value)
                    {
                        translationUnit.Target.Content = value;
                        updatedSegmentsCount++;
                    }
                }
            }

            var fileReference = await _fileManagementClient.UploadAsync(xliffDocument.ConvertToXliff(), input.Xliff.ContentType, input.Xliff.Name);
            return new ProcessXliffResponse { Xliff = fileReference, Usage = entity.Usage, UpdatedSegmentsCount = updatedSegmentsCount, TotalSegmentsCount = translationUnits.Count };
        });
     }
    
    [Action("Post-edit XLIFF", Description = "Updates the targets of XLIFF 1.2 files")]
    public async Task<ProcessXliffResponse> PostEditXliff([ActionParameter] ProcessXliffRequest input,
        [ActionParameter] GlossaryRequest glossaryRequest,[ActionParameter,
         Display("Bucket size",
             Description =
                 "Specify the number of translation units processed at once. Default value: 1500. (See our documentation for an explanation)")]
        int? bucketSize = 1500)
    {
        ThrowIfXliffInvalid(input.Xliff);

        var xliffDocument = await LoadAndParseXliffDocument(input.Xliff);
        var translationUnits = xliffDocument.Files.SelectMany(x => x.TranslationUnits).ToList();
        if (translationUnits.Count == 0)
        {
            return new ProcessXliffResponse { Xliff = input.Xliff };
        }
        
        var entity = await PostEditXliffDocument(input, glossaryRequest, xliffDocument, bucketSize ?? 1500);
        var updatedSegmentsCount = 0;
        foreach (var (key, value) in entity.TranslationUnits)
        {
            var translationUnit = translationUnits.FirstOrDefault(x => x.Id == key);
            if (translationUnit != null)
            {
                if(translationUnit.Target.Content != value)
                {
                    translationUnit.Target.Content = value;
                    updatedSegmentsCount++;
                }
            }
        }
        
        var fileReference = await _fileManagementClient.UploadAsync(xliffDocument.ConvertToXliff(), input.Xliff.ContentType, input.Xliff.Name);
        return new ProcessXliffResponse { Xliff = fileReference, Usage = entity.Usage, UpdatedSegmentsCount = updatedSegmentsCount, TotalSegmentsCount = translationUnits.Count };
    }
    
    [Action("Get Quality Scores for XLIFF", Description = "Gets segment and file level quality scores for XLIFF files")]
    public async Task<ScoreXliffResponse> GetQualityScores([ActionParameter] ProcessXliffRequest input,
        [ActionParameter] GlossaryRequest glossaryRequest)
    {
        ThrowIfXliffInvalid(input.Xliff);

        var xliffDocument = await LoadAndParseXliffDocument(input.Xliff);
        var translationUnits = xliffDocument.Files.SelectMany(x => x.TranslationUnits).ToList();
        if (translationUnits.Count == 0)
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
        var translationUnits = xliff.Files.SelectMany(x => x.TranslationUnits).ToList();
        var batches = translationUnits.Batch(bucketSize);
        
        var totalUsage = new UsageResponse();
        foreach (var batch in batches)
        {
            string json = JsonConvert.SerializeObject(batch.Select(x => "{ID:" + x.Id + "}" + x.Source.Content ));
            var UserPrompt = GetUserPrompt(request.Prompt,xliff,json);
            var response = await _chatActions.CreateCompletion(new(request)
            {
                Prompt = UserPrompt,
                SystemPrompt = request.SystemPrompt ?? "You are tasked with localizing the provided text. Consider cultural nuances, idiomatic expressions, " +
                                "and locale-specific references to make the text feel natural in the target language. " +
                                "Reply only with the serialized JSON array of translated strings without additional formatting. Ensure the structure of the original text is preserved."
            }, glossaryRequest);

            string[] result;
            try
            {
                result = JsonConvert.DeserializeObject<string[]>(response.Text.Substring(response.Text.IndexOf("[")));
            } catch (Exception e) 
            {
                if (e.Message.Contains("Unterminated string. Expected delimiter:")) 
                {
                    throw new PluginApplicationException("Anthropic returned an unexpected response. Try adjusting the model or a lower bucket size or add retries to this action. Original error: " + e.Message);
                }
                else
                {
                    throw new PluginApplicationException(e.Message);
                }
            }
            
            if (result != null && result.Length != translationUnits.Count)
            {
                throw new PluginApplicationException(
                    "Anthropic returned an unexpected response. " +
                    "The number of translated texts does not match the number of source texts. " +
                    "There is probably a duplicated or a missing text in a translation unit. " +
                    "Try adjusting the model or lower bucket size or add retries to this action.");
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
        var translationUnits = xliff.Files.SelectMany(x => x.TranslationUnits).ToList();
        var batches = translationUnits.Batch(bucketSize);

        var totalUsage = new UsageResponse();
        foreach (var batch in batches)
        {
            var response = await _chatActions.CreateCompletion(new(request)
            {
                Prompt = 
                $"Your input consists of sentences in {xliff.SourceLanguage} language with their translations into {xliff.TargetLanguage}. " +
                "Review and edit the translated target text as necessary to ensure it is a correct and accurate translation of the source text. " +
                "The XML tags in the source need to be included in the target text, don't delete or modify them. " +
                "Include only the target texts (including the necessary linguitic updates) in the format [ID:X]{target}. " +
                $"Example: [ID:1]{{target1}},[ID:2]{{target2}}. " +
                $"{request.Prompt ?? ""} Sentences: \n" +
                string.Join("\n", batch.Select(tu => $"ID: {tu.Id}; Source: {tu.Source.Content}; Target: {tu.Target.Content}")),
                SystemPrompt = request.SystemPrompt ?? "You are a linguistic expert that should process the following texts according to the given instructions."
            }, glossaryRequest);

            var matches = Regex.Matches(response.Text.Replace("</ept}", "</ept>}"), @"\[ID:(.+?)\]\{([\s\S]+?)\}(?=,\[|$|,?\n)").Cast<Match>().ToList();
            foreach (var match in matches)
            {
                if (match.Groups[2].Value.Contains("[ID:"))
                {
                    continue;
                }
                else
                {
                    results.Add(match.Groups[1].Value, match.Groups[2].Value);
                }
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
        var translationUnits = xliff.Files.SelectMany(x => x.TranslationUnits).ToList();
        foreach (var translationUnit in translationUnits)
        {
            var sourceLanguage = translationUnit.SourceLanguage ?? xliff.SourceLanguage;
            var targetLanguage = translationUnit.TargetLanguage ?? xliff.TargetLanguage;
            var response = await _chatActions.CreateCompletion(new(request)
            {
                Prompt = $"Your input is going to be a sentence in {sourceLanguage} as source language and their translation into {targetLanguage}. " +
                         "You need to review the target text and respond with scores for the target text. " +
                         $"The score number is a score from 1 to 10 assessing the quality of the translation, considering the following criteria: {criteria}." +
                         $"Provide only number as response, it's crucial, because your response will be displayed to the user without any further processing" +
                         $"Translation unit: \n" +
                         $"Source: {translationUnit.Source.Content}; Target: {translationUnit.Target.Content}",
                SystemPrompt = request.SystemPrompt ?? "You are a linguistic expert that should process the following texts according to the given instructions."
            }, glossaryRequest);
            
            translationUnit.Attributes?.Add("extradata", response.Text);

            string scoreText = response.Text?.Split(' ')[0].Trim();

            if (double.TryParse(scoreText, out double score))
            {
                if (score < 0 || score > 10)
                {
                    throw new PluginApplicationException($"Score out of expected range (0-10). Received: {score}");
                }
            }
            else
            {              
                throw new PluginApplicationException($"Received invalid score from API. Score: {response.Text}");
            }

            totalScore += score;
            totalUsage += response.Usage;
        }
        
        return new(totalScore / translationUnits.Count, totalUsage);
    }

    private async Task<FileReference> UploadUpdatedDocument(XliffDocument xliffDocument, FileReference originalFile)
    {
        var outputMemoryStream = xliffDocument.ConvertToXliff();
        var contentType = originalFile.ContentType ?? "application/xml";
        return await _fileManagementClient.UploadAsync(outputMemoryStream, contentType, originalFile.Name);
    }
}