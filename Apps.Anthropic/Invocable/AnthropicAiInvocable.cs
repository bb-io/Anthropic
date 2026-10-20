using Apps.Anthropic.Ai;
using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Models.Response;
using Blackbird.Ai.Background;
using Blackbird.Ai.Background.Models;
using Blackbird.Ai.Batching.Models;
using Blackbird.Ai.Content.Models;
using Blackbird.Ai.Files.Models;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Blackbird.Ai.Prompts.Models;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Anthropic.Invocable;

// Shared plumbing for the actions that run on the Blackbird.Ai pipeline.
public class AnthropicAiInvocable(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AnthropicInvocable(invocationContext)
{
    private const string LogTag = "[Anthropic]";

    protected IFileManagementClient FileManagementClient { get; } = fileManagementClient;

    // Virtual so a test can substitute or wrap the provider client.
    protected virtual ILlmClient CreateLlmClient(string? model, AnthropicLlmSettings settings) =>
        new AnthropicLlmClient(Client, model, settings);

    // Virtual so a test can replace the batch provider. Message Batches exist on the Anthropic API only.
    protected virtual IBatchLlmClient CreateBatchClient(string? model, AnthropicLlmSettings settings) =>
        Client is ISupportsBatching batchClient
            ? new AnthropicBatchLlmClient(batchClient, model, settings)
            : throw new PluginMisconfigurationException(
                "Background actions use Anthropic Message Batches, which are only available for the 'API Token' connection type.");

    // Starting a job needs the synchronous client for language detection and the summary; completing one does not.
    protected BackgroundContentProcessor CreateBackgroundProcessor(
        IBatchLlmClient batchClient,
        ILlmClient? client = null,
        string? model = null,
        int? bucketSize = null) =>
        new(batchClient, client, CreateEngineOptions(model, bucketSize));

    protected async Task<BackgroundProcessingResponse> ToBackgroundResponseAsync(BackgroundStart started, ILlmClient client)
    {
        var content = started.Content;

        return new BackgroundProcessingResponse
        {
            // With nothing to process no batch is created; the download then returns the file unchanged.
            BatchId = started.Job?.Id ?? string.Empty,
            Status = StatusOf(started.Job),
            CreatedAt = started.Job?.CreatedAt?.UtcDateTime,
            TransformationFile = await UploadAsync(started.WorkFile),
            TotalSegmentsCount = content.TotalSegments,
            SegmentsToProcessCount = content.ProcessableSegments,
            RequestsCount = started.Requests,
            SystemPrompt = content.SystemPrompt ?? string.Empty,
            Usage = ToUsage(content.Usage, client)
        };
    }

    protected static string StatusOf(BatchJob? job) =>
        (job?.State ?? BatchJobState.Completed).ToString().ToLowerInvariant();

    // Full traces keep every user prompt, which the "Prompts" outputs return.
    protected EngineOptions CreateEngineOptions(string? model, int? bucketSize = null) => new()
    {
        Logger = AiAdapters.Logger(InvocationContext),
        LogTag = LogTag,
        ProvenanceTool = string.IsNullOrWhiteSpace(model) ? "Anthropic" : $"Anthropic/{model}",
        Bucketing = BucketingOptions.FromInputs(bucketSize, null),
        TraceLevel = PromptTraceLevel.Full
    };

    protected async Task<Stream?> DownloadOptionalAsync(FileReference? file) =>
        file is null ? null : await FileManagementClient.DownloadAsync(file);

    protected async Task<FileReference> UploadAsync(ContentStream file)
    {
        await using var content = file.Content;
        return await FileManagementClient.UploadAsync(content, file.MediaType, file.FileName);
    }

    protected static UsageResponse ToUsage(LlmUsage usage, ILlmClient client) =>
        client is AnthropicLlmClient anthropicClient
            ? anthropicClient.Usage
            : new UsageResponse { InputTokens = usage.InputTokens, OutputTokens = usage.OutputTokens };

    protected static List<string> UserPrompts(ContentResult content) =>
        content.Traces.Select(trace => trace.UserPrompt).ToList();

    // Text actions have one item; when the model never gave a usable answer there is nothing to return.
    protected static void ThrowIfUnresolved(ContentResult content)
    {
        if (content.UnresolvedSegments > 0)
        {
            throw new PluginApplicationException(string.Join(Environment.NewLine, content.Warnings));
        }
    }
}
