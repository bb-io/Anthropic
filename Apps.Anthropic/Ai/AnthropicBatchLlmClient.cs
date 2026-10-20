using System.Runtime.CompilerServices;
using Apps.Anthropic.Api.Anthropic;
using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Extensions;
using Apps.Anthropic.Models.Entities;
using Apps.Anthropic.Models.Response;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;

namespace Apps.Anthropic.Ai;

// Runs the library's background operations on Anthropic Message Batches. Each request carries the body
// a direct call sends, and each result is read like a direct response.
public sealed class AnthropicBatchLlmClient(ISupportsBatching client, string? model, AnthropicLlmSettings settings) : IBatchLlmClient
{
    private const string EndedStatus = "ended";

    private readonly object _gate = new();
    private UsageResponse _usage = new();

    // Start plans buckets with these, and a batch has no split-retry, so they match the synchronous client.
    public LlmCapabilities Capabilities { get; } = AnthropicLlmClient.CapabilitiesFor(model, settings.MaxTokens);

    // LlmUsage carries only input/output tokens; this keeps the cache counters the action reports.
    public UsageResponse Usage
    {
        get
        {
            lock (_gate)
            {
                return _usage;
            }
        }
    }

    public async Task<BatchJob> SubmitAsync(IReadOnlyList<BatchLlmRequest> requests, CancellationToken cancellationToken)
    {
        var batch = await client.SendBatchRequestsAsync(requests
            .Select(request => (object)new
            {
                custom_id = request.CustomId,
                @params = BaseAnthropicClient.BuildTextPayload(
                    AnthropicLlmClient.ToMessageRequest(request.Request, model, settings))
            })
            .ToList());

        return ToJob(batch);
    }

    public async Task<BatchJob> GetJobAsync(string jobId, CancellationToken cancellationToken) =>
        ToJob(await client.GetBatchStatusAsync(jobId));

    // An ended batch lists every request: succeeded, errored, canceled or expired.
    public async IAsyncEnumerable<BatchLlmResult> GetResultsAsync(
        BatchJob job,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var line in await client.GetBatchRequestsAsync(job.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (line is not null)
            {
                yield return ToResult(line);
            }
        }
    }

    // Anthropic reports in_progress, canceling or ended; how a batch ended follows from its timestamps and counts.
    public static BatchJobState StateOf(BatchResponse batch)
    {
        if (batch.ProcessingStatus != EndedStatus)
        {
            return BatchJobState.Running;
        }

        if (batch.CancelInitiatedAt is not null)
        {
            return BatchJobState.Cancelled;
        }

        if (batch.RequestCounts.Expired > 0)
        {
            return BatchJobState.Expired;
        }

        return batch.RequestCounts is { Succeeded: 0, Errored: > 0 }
            ? BatchJobState.Failed
            : BatchJobState.Completed;
    }

    internal BatchLlmResult ToResult(BatchRequestDto line)
    {
        var result = line.Result;

        switch (result?.Type)
        {
            case "succeeded" when result.Message is { } message:
                var response = new ResponseMessage
                {
                    Text = message.Content?.ExtractText() ?? string.Empty,
                    Usage = message.Usage ?? new UsageResponse(),
                    StopReason = message.StopReason
                };

                lock (_gate)
                {
                    _usage += response.Usage;
                }

                return BatchLlmResult.Succeeded(line.CustomId, AnthropicLlmClient.ToLlmResponse(response));

            case "errored":
                return BatchLlmResult.Failed(
                    line.CustomId,
                    result.Error?.Error?.Message ?? result.Error?.Error?.Type ?? result.Error?.Type ?? "The request failed.");

            case "canceled":
                return BatchLlmResult.Failed(line.CustomId, "The request was canceled before it was processed.");

            case "expired":
                return BatchLlmResult.Failed(line.CustomId, "The batch expired before this request was processed.");

            default:
                return BatchLlmResult.Failed(line.CustomId, $"Unexpected batch result type '{result?.Type}'.");
        }
    }

    private static BatchJob ToJob(BatchResponse batch)
    {
        var counts = batch.RequestCounts;

        return new BatchJob(
            batch.Id,
            StateOf(batch),
            counts.Processing + counts.Succeeded + counts.Errored + counts.Canceled + counts.Expired,
            counts.Succeeded,
            counts.Errored + counts.Canceled + counts.Expired,
            CreatedAt: batch.CreatedAt is { } createdAt ? new DateTimeOffset(AsUtc(createdAt)) : null);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value
    };
}
