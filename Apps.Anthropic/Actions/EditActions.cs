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

[ActionList("Editing")]
public class EditActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AnthropicAiInvocable(invocationContext, fileManagementClient)
{
    [BlueprintActionDefinition(BlueprintAction.EditFile)]
    [Action("Edit", Description = "Edit a translation. This action assumes you have previously translated content in Blackbird through any translation action.")]
    public async Task<EditContentResponse> EditContent(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] EditContentRequest input,
        [ActionParameter] OptionalSkillRequest skillInput)
    {
        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);

        var client = CreateLlmClient(modelIdentifier.Model, new AnthropicLlmSettings(
            input.Temperature, input.TopP, input.TopK, input.MaxTokensToSample, input.StopSequences, skillInput.SkillId));
        var processor = new ContentStreamProcessor(client, CreateEngineOptions(modelIdentifier.Model, input.BucketSize));

        await using var file = await FileManagementClient.DownloadAsync(input.File);
        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var result = await AiAdapters.Mapped(() => processor.EditAsync(new EditStreamOptions
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
            PromptStrategy = CustomSystemPromptStrategy.For(input.SystemPrompt, () => new EditPromptStrategy()),
            Summary = SummaryOptions.FromToggle(input.GenerateDocumentSummary),
            ModifiedBy = input.ModifiedBy,
            ProcessLockedSegments = input.ProcessLockedSegments ?? false
        }));

        var content = result.Content;

        return new EditContentResponse
        {
            File = await UploadAsync(result.File),
            Usage = ToUsage(content.Usage, client),
            Prompts = UserPrompts(content),
            TotalSegmentsReviewed = content.ProcessableSegments,
            TotalWordsReviewed = content.ProcessableWords,
            TotalSegmentsUpdated = content.UpdatedSegments,
            TotalWordsUpdated = content.UpdatedWords
        };
    }

    [BlueprintActionDefinition(BlueprintAction.EditText)]
    [Action("Edit text", Description = "Review translated text and generate an edited version")]
    public async Task<EditTextResponse> EditText(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] EditTextRequest input,
        [ActionParameter] OptionalSkillRequest skillInput)
    {
        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);

        var client = CreateLlmClient(modelIdentifier.Model, new AnthropicLlmSettings(
            input.Temperature, input.TopP, input.TopK, input.MaxTokensToSample, SkillId: skillInput.SkillId));
        var processor = new TextProcessor(client, CreateEngineOptions(modelIdentifier.Model));

        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var result = await AiAdapters.Mapped(() => processor.EditAsync(new EditTextOptions
        {
            SourceText = input.SourceText,
            TargetText = input.TargetText,
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            AdditionalInstructions = Instructions(input),
            GlossaryTbx = glossaryFile
        }));

        ThrowIfUnresolved(result.Content);

        return new EditTextResponse
        {
            EditedText = result.Text,
            SystemPrompt = result.Content.SystemPrompt ?? string.Empty,
            UserPrompt = string.Join(Environment.NewLine, UserPrompts(result.Content)),
            Usage = ToUsage(result.Content.Usage, client)
        };
    }

    private static string? Instructions(EditTextRequest input)
    {
        var audience = string.IsNullOrWhiteSpace(input.TargetAudience)
            ? null
            : $"The target audience is {input.TargetAudience.Trim()}.";

        var instructions = new[] { audience, input.AdditionalInstructions }
            .Where(part => !string.IsNullOrWhiteSpace(part));

        return instructions.Any() ? string.Join(Environment.NewLine, instructions) : null;
    }
}
