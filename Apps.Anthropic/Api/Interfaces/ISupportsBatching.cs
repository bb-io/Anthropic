using Apps.Anthropic.Models.Entities;
using Apps.Anthropic.Models.Response;

namespace Apps.Anthropic.Api.Interfaces;

public interface ISupportsBatching
{
    Task<BatchResponse> SendBatchRequestsAsync(List<object> requests);
    Task<List<BatchRequestDto>> GetBatchRequestsAsync(string batchId); 
    Task<BatchResponse> GetBatchStatusAsync(string batchId);
    Task<List<BatchResponse>> ListBatches();
}
