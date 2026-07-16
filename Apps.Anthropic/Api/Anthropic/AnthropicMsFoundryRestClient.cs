using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;

namespace Apps.Anthropic.Api.Anthropic;

public class AnthropicMsFoundryRestClient(IEnumerable<AuthenticationCredentialsProvider> creds) 
    : BaseAnthropicClient(creds, new Uri($"{creds.Get(CredNames.BaseUrl).Value}/v1")), IAnthropicClient
{
    public override async Task<ResponseMessage> ExecuteChat(MessageRequest message)
    {
        message.Model = creds.Get(CredNames.DeploymentName).Value;
        return await base.ExecuteChat(message);
    }

    public Task<List<ModelResponse>> ListModels()
    {
        throw new PluginMisconfigurationException(
            "Listing models is not supported for this connection type. Please specify the model ID in the connection");
    }

    public async Task<ConnectionValidationResponse> ValidateConnection()
    {
        if (!creds.Get(CredNames.BaseUrl).Value.EndsWith("/anthropic"))
        {
            return new()
            {
                IsValid = false,
                Message = "The endpoint URL must end with '/anthropic'",
            };
        }

        try
        {
            var pingMessage = new MessageRequest
            {
                Messages = [ new() { Role = "user", Content = "Ping! Reply with 'Pong'" } ],
            };

            await ExecuteChat(pingMessage);
            return new() { IsValid = true };
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
}
