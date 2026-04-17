using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Extensions;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Xliff.Utils;
using Blackbird.Xliff.Utils.Models;
using System.Text.RegularExpressions;

namespace Apps.Anthropic.Actions;

[ActionList("Batch processing")]
public class BatchActions : AnthropicInvocable
{
    private readonly ISupportsBatching _batchClient;
    private readonly IFileManagementClient _fileManagementClient;

    public BatchActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
        : base(invocationContext)
    {
        if (Client is not ISupportsBatching batchClient)
        {
            throw new PluginMisconfigurationException(
                "Currently, only the 'Anthropic API token' connection type supports batch actions");
        }

        _batchClient = batchClient;
        _fileManagementClient = fileManagementClient;
    }

    [Action("(Batch) Process XLIFF",
        Description =
            "Asynchronously process each translation unit in the XLIFF file according to the provided instructions (by default it just translates the source tags) and updates the target text for each unit.")]
    public async Task<BatchResponse> ProcessXliffFileAsync(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] ProcessXliffFileRequest request)
    {
        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);

        if (!request.File.Name.EndsWith("xlf", StringComparison.OrdinalIgnoreCase) && !request.File.Name.EndsWith("xliff", StringComparison.OrdinalIgnoreCase) && !request.File.ContentType.Contains("application/x-xliff+xml") && !request.File.ContentType.Contains("application/xliff+xml"))
        {
            throw new PluginMisconfigurationException("File does not have a valid XLIFF extension, please provide a valid XLIFF file.");
        }

        var xliffDocument = await FileManagerHelper.LoadXliffDocument(request.File, _fileManagementClient);
        var instructions = request.Prompt ?? "Translate the text.";
        var requests = await CreateBatchRequestsAsync(
            modelIdentifier,
            xliffDocument,
            request,
            (sourceLang, targetLang) =>
                SystemPromptConstants.ProcessXliffFileWithInstructions(instructions, sourceLang, targetLang),
            (unit, glossaryText) =>
                $"Source: {unit.Source};{(string.IsNullOrEmpty(glossaryText) ? "" : $" {glossaryText}")}"
        );
        return await _batchClient.SendBatchRequestsAsync(requests);
    }

    [Action("(Batch) Post-edit XLIFF",
        Description =
            "Asynchronously post-edit the target text of each translation unit in the XLIFF file according to the provided instructions and updates the target text for each unit.")]
    public async Task<BatchResponse> PostEditXliffFileAsync(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] ProcessXliffFileRequest request)
    {
        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);

        if (!request.File.Name.EndsWith("xlf", StringComparison.OrdinalIgnoreCase) && !request.File.Name.EndsWith("xliff", StringComparison.OrdinalIgnoreCase) && !request.File.ContentType.Contains("application/x-xliff+xml") && !request.File.ContentType.Contains("application/xliff+xml"))
        {
            throw new PluginMisconfigurationException("File does not have a valid XLIFF extension, please provide a valid XLIFF file.");
        }

        var xliffDocument = await FileManagerHelper.LoadXliffDocument(request.File, _fileManagementClient);
        var instructions = request.Prompt
                           ??
                           "Ensure correctness, match to the glossary (if a glossary is provided), and enhance readability and accuracy";
        var requests = await CreateBatchRequestsAsync(
            modelIdentifier,
            xliffDocument,
            request,
            (sourceLang, targetLang) =>
                SystemPromptConstants.PostEditXliffFileWithInstructions(instructions, sourceLang, targetLang),
            (unit, glossaryText) =>
                $"Source: {unit.Source}; Target: {unit.TargetLanguage}{(string.IsNullOrEmpty(glossaryText) ? "" : $" {glossaryText}")}"
        );
        return await _batchClient.SendBatchRequestsAsync(requests);
    }

    [Action("(Batch) Get Quality Scores for XLIFF",
        Description = "Asynchronously get quality scores for each translation unit in the XLIFF file.")]
    public async Task<BatchResponse> GetQualityScoresForXliffFileAsync(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] GetXliffQualityScoreRequest request)
    {
        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);

        if (!request.File.Name.EndsWith("xlf", StringComparison.OrdinalIgnoreCase) && !request.File.Name.EndsWith("xliff", StringComparison.OrdinalIgnoreCase) && !request.File.ContentType.Contains("application/x-xliff+xml") && !request.File.ContentType.Contains("application/xliff+xml"))
        {
            throw new PluginMisconfigurationException("File does not have a valid XLIFF extension, please provide a valid XLIFF file.");
        }

        var xliffDocument = await FileManagerHelper.LoadXliffDocument(request.File, _fileManagementClient);
        var requests = await CreateBatchRequestsAsync(
            modelIdentifier,
            xliffDocument,
            request,
            SystemPromptConstants.EvaluateTranslationQualityWithLanguages,
            (unit, glossaryText) =>
                $"Source: {unit.Source}; Target: {unit.TargetLanguage}{(string.IsNullOrEmpty(glossaryText) ? "" : $" {glossaryText}")}"
        );
        return await _batchClient.SendBatchRequestsAsync(requests);
    }
    
    [Action("(Batch) Get XLIFF from the batch",
        Description = "Get the results of the batch process. This action is suitable only for processing and post-editing XLIFF file and should be called after the async process is ended.")]
    public async Task<GetBatchResultResponse> GetBatchResultsAsync([ActionParameter] GetBatchResultRequest request)
    {
        await ValidateBatchStatus(request.BatchId, _batchClient);
        var batchRequests = await _batchClient.GetBatchRequestsAsync(request.BatchId);
        var xliffDocument = await FileManagerHelper.LoadXliffDocument(request.OriginalXliff, _fileManagementClient);
        var allTranslationUnits = xliffDocument.Files.SelectMany(f => f.TranslationUnits).ToList();
        var totalUsage = new UsageResponse();
        
        foreach (var batchRequest in batchRequests)
        {
            var translationUnit = allTranslationUnits.FirstOrDefault(tu => tu.Id == batchRequest.CustomId);
            if (translationUnit == null)
            {
                throw new PluginApplicationException(
                    $"Translation unit with id {batchRequest.CustomId} not found in the XLIFF file.");
            }

            totalUsage += batchRequest.Result.Message.Usage;
            var newTargetContent = batchRequest.Result.Message.Content.First().Text;
            if (request.AddMissingTrailingTags.HasValue && request.AddMissingTrailingTags == true)
            {
                var sourceContent = translationUnit.Source.Content ?? string.Empty;
                    
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
                        translationUnit.Target.Content = openingTag + newTargetContent + closingTag;
                    }
                    else
                    {
                        translationUnit.Target.Content = newTargetContent;
                    }
                }
                else
                {
                    translationUnit.Target.Content = newTargetContent;
                }
            }
            else
            {
                translationUnit.Target.Content = newTargetContent;
            }
        }

        Stream stream;
        try
        {
            stream = xliffDocument.ConvertToXliff();
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException("Error converting XliffDocument. Please verify that the XLIFF file structure is valid.", ex);
        }

        return new()
        {
            File = await _fileManagementClient.UploadAsync(stream, request.OriginalXliff.ContentType,
                request.OriginalXliff.Name),
            Usage = totalUsage
        };
    }

    [Action("(Batch) Get XLIFF from the quality score batch",
        Description = "Get the quality scores results of the batch process. This action is suitable only for getting quality scores for XLIFF file and should be called after the async process is completed.")]
    public async Task<GetQualityScoreBatchResultResponse> GetQualityScoresResultsAsync(
        [ActionParameter] GetQualityScoreBatchResultRequest request)
    {
        if (!request.OriginalXliff.Name.EndsWith("xlf", StringComparison.OrdinalIgnoreCase) && !request.OriginalXliff.Name.EndsWith("xliff", StringComparison.OrdinalIgnoreCase) && !request.OriginalXliff.ContentType.Contains("application/x-xliff+xml") && !request.OriginalXliff.ContentType.Contains("application/xliff+xml"))
        {
            throw new PluginMisconfigurationException("File does not have a valid XLIFF extension, please provide a valid XLIFF file.");
        }

        await ValidateBatchStatus(request.BatchId, _batchClient);
        var batchRequests = await _batchClient.GetBatchRequestsAsync(request.BatchId);
        var xliffDocument = await FileManagerHelper.LoadXliffDocument(request.OriginalXliff, _fileManagementClient);
        var allTranslationUnits = xliffDocument.Files.SelectMany(f => f.TranslationUnits).ToList();
        var totalScore = 0d;
        foreach (var batchRequest in batchRequests)
        {
            var translationUnit = allTranslationUnits.Find(tu => tu.Id == batchRequest.CustomId);
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
            File = await _fileManagementClient.UploadAsync(xliffDocument.ConvertToXliff(), request.OriginalXliff.ContentType,
                request.OriginalXliff.Name),
            AverageScore = totalScore / batchRequests.Count,
        };
    }

    private static async Task ValidateBatchStatus(string batchId, ISupportsBatching batchClient)
    {
        var batchStatus = await batchClient.GetBatchStatusAsync(batchId);
        if (batchStatus.ProcessingStatus != "ended")
        {
            throw new PluginMisconfigurationException(
                $"The batch process is not completed yet. Current status: {batchStatus.ProcessingStatus}");
        }

        if (batchStatus.RequestCounts.Succeeded == 0)
        {
            throw new PluginApplicationException(
                @"There is no succeded translation units was translated. 
                Please ask support to view and fix the potential issue.");
        }
    }

    private async Task<List<object>> CreateBatchRequestsAsync<T>(
        ModelIdentifier modelIdentifier,
        XliffDocument xliffDocument,
        T request,
        Func<string, string, string> systemPromptFactory,
        Func<XliffUnit, string, string> contentFactory)
        where T : BaseXliffRequest
    {
        var requests = new List<object>();
        var allTranslationUnits = xliffDocument.Files.SelectMany(f => f.TranslationUnits).ToList();
        foreach (var translationUnit in allTranslationUnits)
        {
            var sourceLanguage = translationUnit.SourceLanguage ?? xliffDocument.SourceLanguage;
            var targetLanguage = translationUnit.TargetLanguage ?? xliffDocument.TargetLanguage;
            var glossaryText = "";
            if (request.Glossary != null)
            {
                glossaryText = await GlossaryPromptHelper.GetGlossaryPromptPart(new() { Glossary = request.Glossary }, _fileManagementClient);
            }

            var content = contentFactory(translationUnit, glossaryText);
            requests.Add(new
            {
                custom_id = translationUnit.Id,
                @params = new
                {
                    model = modelIdentifier.Model,
                    max_tokens = request.MaxTokens ?? ModelTokenService.GetMaxTokensForModel(modelIdentifier.Model),
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
}