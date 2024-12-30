using Apps.Anthropic.Api;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Response;
using Apps.OpenAI.Polling.Models;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using RestSharp;

namespace Apps.Anthropic.Polling;

[PollingEventList]
public class BatchPollingList(InvocationContext invocationContext) : AnthropicInvocable(invocationContext, null!)
{
    [PollingEvent("On batch ended", "Triggered when a batch status is set to ended")]
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
                    Triggered = request.Memory?.Triggered ?? false
                }
            };
        }
        
        var getBatchRequest = new RestRequest($"/messages/batches/{identifier.BatchId}");
        var client = new AnthropicRestClient(InvocationContext.AuthenticationCredentialsProviders);
        var batch = await client.ExecuteWithErrorHandling<BatchResponse>(getBatchRequest);
        
        var triggered = batch.ProcessingStatus == "ended" && !request.Memory.Triggered;
        return new()
        {
            FlyBird = triggered,
            Result = batch,
            Memory = new()
            {
                LastPollingTime = DateTime.UtcNow,
                Triggered = triggered
            }
        };
    }
}