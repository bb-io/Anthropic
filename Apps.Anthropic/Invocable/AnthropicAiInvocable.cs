using Apps.Anthropic.Ai;
using Apps.Anthropic.Models.Response;
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
