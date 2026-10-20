using Apps.Anthropic.Ai;
using Apps.Anthropic.Extensions;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Request.Optional;
using Apps.Anthropic.Models.Response;
using Blackbird.Ai.Files;
using Blackbird.Ai.Files.Models;
using Blackbird.Ai.Prompts;
using Blackbird.Ai.Summary.Models;
using Blackbird.Ai.Text;
using Blackbird.Ai.Text.Models;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Anthropic.Actions;

[ActionList("Translation")]
public class TranslationActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AnthropicAiInvocable(invocationContext, fileManagementClient)
{
    [BlueprintActionDefinition(BlueprintAction.TranslateFile)]
    [Action("Translate",
        Description = "Translate file content retrieved from a CMS or file storage. The output can be used in compatible actions.")]
    public async Task<TranslateContentResponse> Translate(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] TranslateContentRequest input,
        [ActionParameter] OptionalSkillRequest skillInput)
    {
        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);

        var client = CreateLlmClient(modelIdentifier.Model, new AnthropicLlmSettings(
            input.Temperature, input.TopP, input.TopK, input.MaxTokensToSample, input.StopSequences, skillInput.SkillId));
        var processor = new ContentStreamProcessor(client, CreateEngineOptions(modelIdentifier.Model, input.BucketSize));

        await using var file = await FileManagementClient.DownloadAsync(input.File);
        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var result = await AiAdapters.Mapped(() => processor.TranslateAsync(TranslateOptions(input, file, glossaryFile)));

        var content = result.Content;

        return new TranslateContentResponse
        {
            File = await UploadAsync(result.File),
            Usage = ToUsage(content.Usage, client),
            Prompts = UserPrompts(content),
            TotalSegmentsCount = content.TotalSegments,
            TotalWordsCount = content.TotalWords,
            UpdatedSegmentsCount = content.UpdatedSegments,
            UpdatedWordsCount = content.UpdatedWords
        };
    }

    [Action("Translate in background",
        Description = "Starts background translation of a file through Anthropic Message Batches and outputs a batch ID and a transformation file to download the result later.")]
    public async Task<BackgroundProcessingResponse> TranslateInBackground(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] TranslateContentRequest input)
    {
        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);

        var settings = new AnthropicLlmSettings(
            input.Temperature, input.TopP, input.TopK, input.MaxTokensToSample, input.StopSequences);
        var batchClient = CreateBatchClient(modelIdentifier.Model, settings);
        var client = CreateLlmClient(modelIdentifier.Model, settings);
        var processor = CreateBackgroundProcessor(batchClient, client, modelIdentifier.Model, input.BucketSize);

        await using var file = await FileManagementClient.DownloadAsync(input.File);
        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var started = await AiAdapters.Mapped(() => processor.StartTranslateAsync(TranslateOptions(input, file, glossaryFile)));

        return await ToBackgroundResponseAsync(started, client);
    }

    // Shared by "Translate" and "Translate in background", so the two cannot drift apart.
    private static TranslateStreamOptions TranslateOptions(TranslateContentRequest input, Stream file, Stream? glossaryFile) => new()
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
        PromptStrategy = CustomSystemPromptStrategy.For(input.SystemPrompt, () => new TranslationPromptStrategy()),
        Summary = SummaryOptions.FromToggle(input.GenerateDocumentSummary)
    };

    [BlueprintActionDefinition(BlueprintAction.TranslateText)]
    [Action("Translate text", Description = "Localize the text provided.")]
    public async Task<TranslateTextResponse> TranslateText(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] TranslateTextRequest input,
        [ActionParameter] OptionalSkillRequest skillInput)
    {
        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);

        var client = CreateLlmClient(modelIdentifier.Model, new AnthropicLlmSettings(
            input.Temperature, input.TopP, input.TopK, input.MaxTokensToSample, SkillId: skillInput.SkillId));
        var processor = new TextProcessor(client, CreateEngineOptions(modelIdentifier.Model));

        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var result = await AiAdapters.Mapped(() => processor.TranslateAsync(new TranslateTextOptions
        {
            Text = input.Text,
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            AdditionalInstructions = input.AdditionalInstructions,
            GlossaryTbx = glossaryFile
        }));

        ThrowIfUnresolved(result.Content);

        return new TranslateTextResponse
        {
            TranslatedText = result.Text,
            SystemPrompt = result.Content.SystemPrompt ?? string.Empty,
            UserPrompt = string.Join(Environment.NewLine, UserPrompts(result.Content)),
            Usage = ToUsage(result.Content.Usage, client)
        };
    }
}
