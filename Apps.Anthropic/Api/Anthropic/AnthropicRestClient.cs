using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Models.Entities;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Anthropic.Api.Anthropic;

public class AnthropicRestClient(IEnumerable<AuthenticationCredentialsProvider> creds) 
    : BaseAnthropicClient(creds, new Uri("https://api.anthropic.com/v1")), IAnthropicClient, ISupportsBatching
{
    public async Task<ConnectionValidationResponse> ValidateConnection()
    {
        var request = new RestRequest("/models", Method.Get);

        try
        {
            var response = await ExecuteWithErrorHandling(request);
            return new()
            {
                IsValid = response.IsSuccessful,
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }
    }

    public async Task<List<ModelResponse>> ListModels()
    {
        var request = new RestRequest("/models");
        var models = await ExecuteWithErrorHandling<DataResponse<ModelResponse>>(request);
        return models.Data.Select(x => new ModelResponse(x.Id, x.DisplayName)).ToList();
    }

    public async Task<BatchResponse> SendBatchRequestsAsync(List<object> requests)
    {
        var request = new RestRequest("/messages/batches", Method.Post).WithJsonBody(new { requests });
        var batch = await ExecuteWithErrorHandling<BatchResponse>(request);
        return batch;
    }

    public async Task<BatchResponse> GetBatchStatusAsync(string batchId)
    {
        var request = new RestRequest($"/messages/batches/{batchId}");
        return await ExecuteWithErrorHandling<BatchResponse>(request);
    }

    public async Task<List<BatchRequestDto>> GetBatchRequestsAsync(string batchId)
    {
        var fileContentResponse = await ExecuteWithErrorHandling(new RestRequest($"/messages/batches/{batchId}/results"));

        var batchRequests = new List<BatchRequestDto>();
        using var reader = new StringReader(fileContentResponse.Content!);
        while (await reader.ReadLineAsync() is { } line)
        {
            var batchRequest = JsonConvert.DeserializeObject<BatchRequestDto>(line)!;
            batchRequests.Add(batchRequest);
        }

        return batchRequests;
    }
}