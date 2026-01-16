using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.Anthropic.Api;

public interface IAnthropicClient
{
    Task<ConnectionValidationResponse> ValidateConnection();
    Task<ResponseMessage> ExecuteChat(MessageRequest message);
    Task<List<ModelResponse>> ListModels();
}
