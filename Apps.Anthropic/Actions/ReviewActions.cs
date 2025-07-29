using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Utils;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using System.Xml.Linq;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Extensions;

namespace Apps.Anthropic.Actions;

[ActionList("Review")]
public class ReviewActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : AnthropicInvocable(invocationContext, fileManagementClient)
{
    private readonly AiUtilities _aiUtilities = new(invocationContext, fileManagementClient);

    [BlueprintActionDefinition(BlueprintAction.ReviewFile)]
    [Action("Review", Description = "Review translation. This action assumes you have previously translated content in Blackbird through any translation action.")]
    public async Task<ReviewContentResponse> ReviewContent([ActionParameter] ReviewContentRequest input)
    {
        var result = new ReviewContentResponse();

        var stream = await fileManagementClient.DownloadAsync(input.File);
        var content =
            await ErrorHandler.ExecuteWithErrorHandlingAsync(() => Transformation.Parse(stream, input.File.Name));
        content.SourceLanguage ??= input.SourceLanguage;
        content.TargetLanguage ??= input.TargetLanguage;

        var segments = content.GetSegments().Where(x => !x.IsIgnorbale && !string.IsNullOrWhiteSpace(x.GetTarget()))
            .ToList();
        result.TotalSegmentsProcessed = segments.Count;

        var totalScore = 0.0f;
        var finalizedSegments = 0;
        var underThresholdSegments = 0;

        foreach (var segment in segments)
        {
            var reviewData = new List<object>
            {
                new
                {
                    translation_id = segment.Id,
                    source_text = segment.GetSource(),
                    target_text = segment.GetTarget()
                }
            };

            var json = JsonConvert.SerializeObject(reviewData);
            var userPrompt = PromptBuilder.BuildReviewUserPrompt(input.AdditionalInstructions, content.SourceLanguage, content.TargetLanguage, json);
            var systemPrompt = PromptBuilder.BuildReviewSystemPrompt();

            var completionRequest = new CompletionRequest
            {
                Model = input.Model,
                Prompt = userPrompt,
                SystemPrompt = systemPrompt,
                MaxTokensToSample = input.MaxTokensToSample,
                Temperature = input.Temperature,
                TopP = input.TopP,
                TopK = input.TopK,
                StopSequences = input.StopSequences
            };

            var response = await _aiUtilities.SendMessageAsync(completionRequest, new() { Glossary = input.Glossary });
            result.Usage += response.Usage;

            var deserializationResult = ResponseDeserializationHelper.DeserializeReviewResponse(response.Text);

            if (deserializationResult.IsSuccess && deserializationResult.Reviews.Count > 0)
            {
                var review = deserializationResult.Reviews.First();
                var qualityScore = review.QualityScore;

                segment.TargetAttributes.RemoveAll(attr => attr.Name == "extradata");
                segment.TargetAttributes.Add(new XAttribute("extradata", qualityScore.ToString()));

                totalScore += qualityScore;

                if (qualityScore >= 0.8f)
                    finalizedSegments++;
                if (qualityScore < 0.6f)
                    underThresholdSegments++;
            }
        }

        result.TotalSegmentsFinalized = finalizedSegments;
        result.TotalSegmentsUnderThreshhold = underThresholdSegments;
        result.AverageMetric = segments.Count > 0 ? totalScore / segments.Count : 0;
        result.PercentageSegmentsUnderThreshhold =
            segments.Count > 0 ? (float)underThresholdSegments / segments.Count * 100 : 0;

        if (input.OutputFileHandling == "original")
        {
            var targetContent = content.Target();
            result.File = await fileManagementClient.UploadAsync(targetContent.Serialize().ToStream(),
                targetContent.OriginalMediaType, targetContent.OriginalName);
        }
        else
        {
            result.File = await fileManagementClient.UploadAsync(content.Serialize().ToStream(), MediaTypes.Xliff,
                content.XliffFileName);
        }

        return result;
    }

    [Action("Review text", Description = "Review the quality of translated text.")]
    public async Task<ReviewTextResponse> ReviewText([ActionParameter] ReviewTextRequest input)
    {
        var reviewData = new List<object>
        {
            new
            {
                translation_id = "1",
                source_text = input.SourceText,
                target_text = input.TargetText
            }
        };

        var json = JsonConvert.SerializeObject(reviewData);
        
        var userPrompt = PromptBuilder.BuildReviewUserPrompt(input.AdditionalInstructions, input.SourceLanguage, input.TargetLanguage, json);
        var systemPrompt = PromptBuilder.BuildReviewSystemPrompt();

        var completionRequest = new CompletionRequest
        {
            Model = input.Model,
            Prompt = userPrompt,
            SystemPrompt = systemPrompt,
            MaxTokensToSample = input.MaxTokensToSample,
            Temperature = input.Temperature,
            TopP = input.TopP,
            TopK = input.TopK
        };

        var response = await _aiUtilities.SendMessageAsync(completionRequest, new() { Glossary = input.Glossary });
        var deserializationResult = ResponseDeserializationHelper.DeserializeReviewResponse(response.Text);

        float qualityScore = 0.0f;
        if (deserializationResult.IsSuccess && deserializationResult.Reviews.Count > 0)
        {
            var review = deserializationResult.Reviews.First();
            qualityScore = review.QualityScore;
        }

        return new ReviewTextResponse
        {
            Score = qualityScore,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Usage = response.Usage
        };
    }
}