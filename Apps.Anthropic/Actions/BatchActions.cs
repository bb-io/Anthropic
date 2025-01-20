using System.Text.RegularExpressions;
using Apps.Anthropic.Api;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Entities;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Xliff.Utils;
using Blackbird.Xliff.Utils.Models;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Anthropic.Actions;

[ActionList]
public class BatchActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AnthropicInvocable(invocationContext, fileManagementClient)
{
    [Action("(Batch) Process XLIFF file",
        Description =
            "Asynchronously process each translation unit in the XLIFF file according to the provided instructions (by default it just translates the source tags) and updates the target text for each unit.")]
    public async Task<BatchResponse> ProcessXliffFileAsync([ActionParameter] ProcessXliffFileRequest request)
    {
        var xliffDocument = await LoadAndParseXliffDocument(request.File);
        var instructions = request.Prompt ?? "Translate the text.";
        var requests = await CreateBatchRequestsAsync(
            xliffDocument,
            request,
            (sourceLang, targetLang) =>
                SystemPromptConstants.ProcessXliffFileWithInstructions(instructions, sourceLang, targetLang),
            (unit, glossaryText) =>
                $"Source: {unit.Source};{(string.IsNullOrEmpty(glossaryText) ? "" : $" {glossaryText}")}"
        );
        return await SendBatchRequestsAsync(requests);
    }

    [Action("(Batch) Post-edit XLIFF file",
        Description =
            "Asynchronously post-edit the target text of each translation unit in the XLIFF file according to the provided instructions and updates the target text for each unit.")]
    public async Task<BatchResponse> PostEditXliffFileAsync([ActionParameter] ProcessXliffFileRequest request)
    {
        var xliffDocument = await LoadAndParseXliffDocument(request.File);
        var instructions = request.Prompt
                           ??
                           "Ensure correctness, match to the glossary (if a glossary is provided), and enhance readability and accuracy";
        var requests = await CreateBatchRequestsAsync(
            xliffDocument,
            request,
            (sourceLang, targetLang) =>
                SystemPromptConstants.PostEditXliffFileWithInstructions(instructions, sourceLang, targetLang),
            (unit, glossaryText) =>
                $"Source: {unit.Source}; Target: {unit.TargetLanguage}{(string.IsNullOrEmpty(glossaryText) ? "" : $" {glossaryText}")}"
        );
        return await SendBatchRequestsAsync(requests);
    }

    [Action("(Batch) Get Quality Scores for XLIFF file",
        Description = "Asynchronously get quality scores for each translation unit in the XLIFF file.")]
    public async Task<BatchResponse> GetQualityScoresForXliffFileAsync(
        [ActionParameter] GetXliffQualityScoreRequest request)
    {
        var xliffDocument = await LoadAndParseXliffDocument(request.File);
        var requests = await CreateBatchRequestsAsync(
            xliffDocument,
            request,
            (sourceLang, targetLang) =>
                SystemPromptConstants.EvaluateTranslationQualityWithLanguages(sourceLang, targetLang),
            (unit, glossaryText) =>
                $"Source: {unit.Source}; Target: {unit.TargetLanguage}{(string.IsNullOrEmpty(glossaryText) ? "" : $" {glossaryText}")}"
        );
        return await SendBatchRequestsAsync(requests);
    }
    
    [Action("(Batch) Get XLIFF from the batch",
        Description = "Get the results of the batch process. This action is suitable only for processing and post-editing XLIFF file and should be called after the async process is ended.")]
    public async Task<GetBatchResultResponse> GetBatchResultsAsync([ActionParameter] GetBatchResultRequest request)
    {
        var batchRequests = await GetBatchRequestsAsync(request.BatchId);
        var xliffDocument = await LoadAndParseXliffDocument(request.OriginalXliff);
        var totalUsage = new UsageResponse();
        
        foreach (var batchRequest in batchRequests)
        {
            var translationUnit = xliffDocument.TranslationUnits.FirstOrDefault(tu => tu.Id == batchRequest.CustomId);
            if (translationUnit == null)
            {
                throw new PluginApplicationException(
                    $"Translation unit with id {batchRequest.CustomId} not found in the XLIFF file.");
            }

            totalUsage += batchRequest.Result.Message.Usage;
            var newTargetContent = batchRequest.Result.Message.Content.First().Text;
            if (request.AddMissingTrailingTags.HasValue && request.AddMissingTrailingTags == true)
            {
                var sourceContent = translationUnit.Source!;
                    
                var tagPattern = @"<(?<tag>\w+)(?<attributes>[^>]*)>(?<content>.*?)</\k<tag>>";
                var sourceMatch = Regex.Match(sourceContent, tagPattern, RegexOptions.Singleline);

                if (sourceMatch.Success)
                {
                    var tagName = sourceMatch.Groups["tag"].Value;
                    var tagAttributes = sourceMatch.Groups["attributes"].Value;
                    var openingTag = $"<{tagName}{tagAttributes}>";
                    var closingTag = $"</{tagName}>";

                    if (!newTargetContent.Contains(openingTag) && !newTargetContent.Contains(closingTag))
                    {
                        translationUnit.Target = openingTag + newTargetContent + closingTag;
                    }
                    else
                    {
                        translationUnit.Target = newTargetContent;
                    }
                }
                else
                {
                    translationUnit.Target = newTargetContent;
                }
            }
            else
            {
                translationUnit.Target = newTargetContent;
            }
        }

        var stream = xliffDocument.ToStream();
        return new()
        {
            File = await fileManagementClient.UploadAsync(stream, request.OriginalXliff.ContentType,
                request.OriginalXliff.Name),
            Usage = totalUsage
        };
    }

    [Action("(Batch) Get XLIFF from the quality score batch",
        Description = "Get the quality scores results of the batch process. This action is suitable only for getting quality scores for XLIFF file and should be called after the async process is completed.")]
    public async Task<GetQualityScoreBatchResultResponse> GetQualityScoresResultsAsync(
        [ActionParameter] GetQualityScoreBatchResultRequest request)
    {
        var batchRequests = await GetBatchRequestsAsync(request.BatchId);
        var xliffDocument = await LoadAndParseXliffDocument(request.OriginalXliff);
        var totalScore = 0d;
        foreach (var batchRequest in batchRequests)
        {
            var translationUnit = xliffDocument.TranslationUnits.Find(tu => tu.Id == batchRequest.CustomId);
            if (translationUnit == null)
            {
                throw new PluginApplicationException(
                    $"Translation unit with id {batchRequest.CustomId} not found in the XLIFF file.");
            }

            var currentContent = batchRequest.Result.Message.Content.First().Text;
            if (double.TryParse(currentContent, out var score))
            {
                totalScore += score;
                translationUnit.Attributes.Add("extradata", currentContent);
            }
            else if (request.ThrowExceptionOnAnyUnexpectedResult.HasValue &&
                     request.ThrowExceptionOnAnyUnexpectedResult.Value)
            {
                throw new PluginApplicationException(
                    $"The quality score for translation unit with id {batchRequest.CustomId} is not a valid number. " +
                    $"Value: {currentContent}");
            }
            else
            {
                translationUnit.Attributes.Add("extradata", "0");
            }
        }

        return new()
        {
            File = await fileManagementClient.UploadAsync(xliffDocument.ToStream(), request.OriginalXliff.ContentType,
                request.OriginalXliff.Name),
            AverageScore = totalScore / batchRequests.Count,
        };
    }

    private async Task<List<object>> CreateBatchRequestsAsync<T>(
        XliffDocument xliffDocument,
        T request,
        Func<string, string, string> systemPromptFactory,
        Func<TranslationUnit, string, string> contentFactory)
        where T : BaseXliffRequest
    {
        var requests = new List<object>();
        foreach (var translationUnit in xliffDocument.TranslationUnits)
        {
            var sourceLanguage = translationUnit.SourceLanguage ?? xliffDocument.SourceLanguage;
            var targetLanguage = translationUnit.TargetLanguage ?? xliffDocument.TargetLanguage;
            var glossaryText = "";
            if (request.Glossary != null)
            {
                glossaryText = await GetGlossaryPromptPart(new() { Glossary = request.Glossary });
            }

            var content = contentFactory(translationUnit, glossaryText);
            requests.Add(new
            {
                custom_id = translationUnit.Id,
                @params = new
                {
                    model = request.Model,
                    max_tokens = request.MaxTokens ?? 4096,
                    messages = new List<object>
                    {
                        new
                        {
                            role = "user",
                            content = systemPromptFactory(sourceLanguage, targetLanguage)
                        },
                        new
                        {
                            role = "user",
                            content
                        }
                    }
                }
            });
        }

        return requests;
    }

    private async Task<BatchResponse> SendBatchRequestsAsync(List<object> requests)
    {
        var client = new AnthropicRestClient(InvocationContext.AuthenticationCredentialsProviders);
        var apiRequest = new RestRequest("/messages/batches", Method.Post)
            .WithJsonBody(new { requests });
        var batch = await client.ExecuteWithErrorHandling<BatchResponse>(apiRequest);
        return batch;
    }
    
    private async Task<List<BatchRequestDto>> GetBatchRequestsAsync(string batchId)
    {
        var getBatchRequest = new RestRequest($"/messages/batches/{batchId}");
        var client = new AnthropicRestClient(InvocationContext.AuthenticationCredentialsProviders);
        
        var batch = await client.ExecuteWithErrorHandling<BatchResponse>(getBatchRequest);
    
        if (batch.ProcessingStatus != "ended")
        {
            throw new PluginMisconfigurationException(
                $"The batch process is not completed yet. Current status: {batch.ProcessingStatus}");
        }
        
        if(batch.RequestCounts.Succeeded == 0)
        {
            throw new PluginApplicationException(
                $"There is no succeded translation units was translated. Please ask support to view and fix the potential issue.");
        }

        var fileContentResponse = await client.ExecuteWithErrorHandling(
            new RestRequest($"/messages/batches/{batchId}/results"));

        var batchRequests = new List<BatchRequestDto>();
        using var reader = new StringReader(fileContentResponse.Content!);
        while (await reader.ReadLineAsync() is { } line)
        {
            var batchRequest = JsonConvert.DeserializeObject<BatchRequestDto>(line)!;
            batchRequests.Add(batchRequest);
        }

        return batchRequests;
    }
}