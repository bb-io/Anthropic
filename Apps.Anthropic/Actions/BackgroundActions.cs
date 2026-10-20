using Apps.Anthropic.Ai;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Ai.Background;
using Blackbird.Ai.Background.Models;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Blackbird.Ai.Prompts.Models;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Anthropic.Actions;

[ActionList("Background")]
public class BackgroundActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AnthropicAiInvocable(invocationContext, fileManagementClient)
{
    [Action("Download background file",
        Description = "Downloads content processed in the background once the batch has finished. Results of failed, expired or canceled requests are left unchanged and counted as unresolved.")]
    public async Task<BackgroundContentResponse> DownloadContentFromBackground([ActionParameter] BackgroundDownloadRequest request)
    {
        await using var file = await DownloadToMemoryAsync(request.TransformationFile);
        var job = BackgroundJobFile.TryRead(file, request.TransformationFile.Name, request.TransformationFile.ContentType);
        file.Position = 0;

        if (job is null)
        {
            throw new PluginMisconfigurationException(
                "This file holds no background job. Pass the transformation file returned by 'Translate in background', " +
                "'Edit in background' or 'Review in background'.");
        }

        var batchClient = CreateBatchClient(null, new AnthropicLlmSettings());
        var result = await AiAdapters.Mapped(() => CreateBackgroundProcessor(batchClient).CompleteAsync(new CompleteBackgroundOptions
        {
            File = file,
            FileName = request.TransformationFile.Name,
            MediaType = request.TransformationFile.ContentType,
            JobId = string.IsNullOrWhiteSpace(request.BatchId) ? null : request.BatchId.Trim(),
            OutputFileHandling = request.OutputFileHandling
        }));

        var content = result.Content;

        return new BackgroundContentResponse
        {
            File = await UploadAsync(result.File),
            Usage = ToBatchUsage(content.Usage, batchClient),
            TotalSegmentsCount = content.TotalSegments,
            ProcessedSegmentsCount = content.ProcessableSegments - content.UnresolvedSegments,
            UpdatedSegmentsCount = content.UpdatedSegments,
            UnresolvedSegmentsCount = content.UnresolvedSegments,
            BatchStatus = StatusOf(result.Job),
            AverageScore = job.Operation == ContentOperation.Review && content.Review is { ScoredSegments: > 0 } review
                ? (float)review.AverageScore
                : null
        };
    }

    // The library's total is the preparation at start plus the batch. Only the batch reports cache
    // counters, so they come from the batch client and the input/output totals from the library.
    private static UsageResponse ToBatchUsage(LlmUsage total, IBatchLlmClient batchClient)
    {
        if (batchClient is not AnthropicBatchLlmClient anthropicBatchClient)
        {
            return new UsageResponse { InputTokens = total.InputTokens, OutputTokens = total.OutputTokens };
        }

        var batch = anthropicBatchClient.Usage;

        return new UsageResponse
        {
            InputTokens = total.InputTokens,
            OutputTokens = total.OutputTokens,
            CacheCreationInputTokens = batch.CacheCreationInputTokens,
            CacheReadInputTokens = batch.CacheReadInputTokens
        };
    }

    // TryRead loads the whole file, and the download stream cannot be rewound.
    private async Task<MemoryStream> DownloadToMemoryAsync(FileReference file)
    {
        await using var downloaded = await FileManagementClient.DownloadAsync(file);
        var buffer = new MemoryStream();
        await downloaded.CopyToAsync(buffer);
        buffer.Position = 0;
        return buffer;
    }
}
