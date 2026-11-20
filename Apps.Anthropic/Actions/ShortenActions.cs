using Apps.Anthropic.Ai;
using Apps.Anthropic.Extensions;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Ai.Content.Models;
using Blackbird.Ai.Files;
using Blackbird.Ai.Files.Models;
using Blackbird.Ai.Text;
using Blackbird.Ai.Text.Models;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Anthropic.Actions;

[ActionList("Shortening")]
public class ShortenActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AnthropicAiInvocable(invocationContext, fileManagementClient)
{
    [Action("Shorten content", Description = "Shorten translated file content to fit the size restrictions of its units or the maximum length.")]
    public async Task<ShortenContentResponse> ShortenContent(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] ShortenContentRequest input)
    {
        ThrowIfInvalid(input.MaximumLength, input.Retries);

        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);
        var model = modelIdentifier.Model;
        var client = CreateLlmClient(model, new());
        var processor = new ContentStreamProcessor(client, CreateEngineOptions(model, input.BucketSize));

        await using var file = await FileManagementClient.DownloadAsync(input.File);
        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var result = await AiAdapters.Mapped(() => processor.ShortenAsync(new ShortenStreamOptions
        {
            File = file,
            FileName = input.File.Name,
            MediaType = input.File.ContentType,
            OutputFileHandling = input.OutputFileHandling,
            GlossaryTbx = glossaryFile,
            AdditionalInstructions = input.AdditionalInstructions,
            MaxLength = input.MaximumLength,
            MaxRetries = input.Retries ?? ShortenOptions.DefaultMaxRetries
        }));

        var shorten = result.Content.Shorten!;

        return new ShortenContentResponse
        {
            File = await UploadAsync(result.File),
            UnitsWithLimit = shorten.UnitsWithLimit,
            UnitsOverLimit = shorten.UnitsOverLimit,
            UnitsShortened = shorten.UnitsShortened,
            UnitsStillOverLimit = shorten.UnitsStillOverLimit,
            Usage = ToUsage(result.Content.Usage, client)
        };
    }

    [Action("Shorten text", Description = "Shorten text to fit the maximum length.")]
    public async Task<ShortenTextResponse> ShortenText(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] ShortenTextRequest input)
    {
        ThrowIfInvalid(input.MaximumLength, input.Retries);

        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);
        var model = modelIdentifier.Model;
        var client = CreateLlmClient(model, new());
        var processor = new TextProcessor(client, CreateEngineOptions(model));

        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var result = await AiAdapters.Mapped(() => processor.ShortenAsync(new ShortenTextOptions
        {
            Text = input.Text,
            MaxLength = input.MaximumLength,
            MaxRetries = input.Retries ?? ShortenOptions.DefaultMaxRetries,
            AdditionalInstructions = input.AdditionalInstructions,
            GlossaryTbx = glossaryFile
        }));

        ThrowIfUnresolved(result.Content);

        return new ShortenTextResponse
        {
            ShortenedText = result.Text,
            SystemPrompt = result.Content.SystemPrompt ?? string.Empty,
            Usage = ToUsage(result.Content.Usage, client)
        };
    }

    private static void ThrowIfInvalid(int? maximumLength, int? retries)
    {
        if (maximumLength is <= 0)
            throw new PluginMisconfigurationException("Maximum length must be greater than zero.");
        if (retries is < 0)
            throw new PluginMisconfigurationException("Retries must be zero or greater.");
    }
}
