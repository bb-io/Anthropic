using Apps.Anthropic.Ai;
using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Response;
using Apps.OpenAI.Polling.Models;
using Blackbird.Ai.Llm.Models;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;

namespace Apps.Anthropic.Polling;

[PollingEventList]
public class BatchPollingList(InvocationContext invocationContext) : AnthropicInvocable(invocationContext)
{
    [PollingEvent("On background job finished", "Triggered once when a batch ends: completed, failed, expired or canceled.")]
    public async Task<PollingEventResponse<BatchMemory, BatchResponse>> OnBatchFinished(
        PollingEventRequest<BatchMemory> request,
        [PollingEventParameter] BatchIdentifier identifier)
    {
        if (request.Memory is null)
        {
            return new()
            {
                FlyBird = false,
                Memory = new()
                {
                    LastPollingTime = DateTime.UtcNow,
                    Triggered = false
                }
            };
        }

        // A background action with nothing to process creates no batch and returns an empty batch ID.
        var batch = string.IsNullOrWhiteSpace(identifier.BatchId)
            ? new BatchResponse { ProcessingStatus = "ended" }
            : await BatchClient().GetBatchStatusAsync(identifier.BatchId.Trim());
        var finished = AnthropicBatchLlmClient.StateOf(batch) != BatchJobState.Running;

        return new()
        {
            FlyBird = finished && !request.Memory.Triggered,
            Result = batch,
            Memory = new()
            {
                LastPollingTime = DateTime.UtcNow,
                // Sticky: once fired, the event never fires again for this batch.
                Triggered = request.Memory.Triggered || finished
            }
        };
    }

    private ISupportsBatching BatchClient() =>
        Client as ISupportsBatching ?? throw new PluginMisconfigurationException(
            "Background jobs use Anthropic Message Batches, which are only available for the 'API Token' connection type.");
}
