using Apps.Anthropic.Ai;
using Apps.Anthropic.Extensions;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Request.Optional;
using Apps.Anthropic.Models.Response;
using Blackbird.Ai.Files;
using Blackbird.Ai.Files.Models;
using Blackbird.Ai.Summary.Models;
using Blackbird.Ai.Text;
using Blackbird.Ai.Text.Models;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Enums;

namespace Apps.Anthropic.Actions;

[ActionList("Review")]
public class ReviewActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AnthropicAiInvocable(invocationContext, fileManagementClient)
{
    // Review has always scored every segment with a target, whatever its state, locked units included.
    private static readonly IReadOnlySet<SegmentState> AllSegmentStates = Enum.GetValues<SegmentState>().ToHashSet();

    [BlueprintActionDefinition(BlueprintAction.ReviewFile)]
    [Action("Review", Description = "Review translation. This action assumes you have previously translated content in Blackbird through any translation action.")]
    public async Task<ReviewContentResponse> ReviewContent(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] ReviewContentRequest input,
        [ActionParameter] OptionalSkillRequest skillInput)
    {
        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);

        var client = CreateLlmClient(modelIdentifier.Model, new AnthropicLlmSettings(
            input.Temperature, input.TopP, input.TopK, input.MaxTokensToSample, input.StopSequences, skillInput.SkillId));
        var processor = new ContentStreamProcessor(client, CreateEngineOptions(modelIdentifier.Model));

        await using var file = await FileManagementClient.DownloadAsync(input.File);
        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var result = await AiAdapters.Mapped(() => processor.ReviewAsync(new ReviewStreamOptions
        {
            File = file,
            FileName = input.File.Name,
            MediaType = input.File.ContentType,
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            OutputFileHandling = input.OutputFileHandling,
            GlossaryTbx = glossaryFile,
            FilterGlossary = input.FilterGlossary ?? true,
            AdditionalInstructions = input.AdditionalInstructions,
            Summary = SummaryOptions.FromToggle(input.GenerateDocumentSummary),
            SegmentStates = AllSegmentStates,
            SegmentFilter = segment => !string.IsNullOrWhiteSpace(segment.GetTarget()),
            ProcessLockedSegments = true
        }));

        var content = result.Content;
        var review = content.Review!;

        return new ReviewContentResponse
        {
            File = await UploadAsync(result.File),
            Usage = ToUsage(content.Usage, client),
            Prompts = UserPrompts(content),
            TotalSegmentsProcessed = content.ProcessableSegments,
            TotalSegmentsFinalized = review.Finalized,
            TotalSegmentsUnderThreshhold = review.UnderThreshold,
            AverageMetric = (float)review.AverageScore,
            PercentageSegmentsUnderThreshhold = (float)review.PercentageUnderThreshold
        };
    }

    [BlueprintActionDefinition(BlueprintAction.ReviewText)]
    [Action("Review text", Description = "Review the quality of translated text.")]
    public async Task<ReviewTextResponse> ReviewText(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] ReviewTextRequest input,
        [ActionParameter] OptionalSkillRequest skillInput)
    {
        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);

        if (string.IsNullOrWhiteSpace(input.SourceText) || string.IsNullOrWhiteSpace(input.TargetText))
        {
            throw new PluginMisconfigurationException("Both the source text and the target text are required for a review.");
        }

        var client = CreateLlmClient(modelIdentifier.Model, new AnthropicLlmSettings(
            input.Temperature, input.TopP, input.TopK, input.MaxTokensToSample, SkillId: skillInput.SkillId));
        var processor = new TextProcessor(client, CreateEngineOptions(modelIdentifier.Model));

        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var result = await AiAdapters.Mapped(() => processor.ReviewAsync(new ReviewTextOptions
        {
            SourceText = input.SourceText,
            TargetText = input.TargetText,
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            AdditionalInstructions = input.AdditionalInstructions,
            GlossaryTbx = glossaryFile
        }));

        ThrowIfUnresolved(result.Content);

        return new ReviewTextResponse
        {
            Score = (float)(result.Score ?? throw new PluginApplicationException("The model returned no quality score.")),
            SystemPrompt = result.Content.SystemPrompt ?? string.Empty,
            UserPrompt = string.Join(Environment.NewLine, UserPrompts(result.Content)),
            Usage = ToUsage(result.Content.Usage, client)
        };
    }
}
