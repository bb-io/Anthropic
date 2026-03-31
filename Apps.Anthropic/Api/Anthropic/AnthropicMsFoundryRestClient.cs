using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using RestSharp;

namespace Apps.Anthropic.Api.Anthropic;

public class AnthropicMsFoundryRestClient(IEnumerable<AuthenticationCredentialsProvider> creds) 
    : BaseAnthropicClient(creds, new Uri($"{creds.Get(CredNames.BaseUrl).Value}/v1")), IAnthropicClient
{
    private readonly IEnumerable<AuthenticationCredentialsProvider> _creds = creds;

    public async Task<List<ModelResponse>> ListModels()
    {
        throw new PluginMisconfigurationException(
            "Listing models is not supported for this connection type. Please specify the model ID in the connection");
    }

    public async Task<ConnectionValidationResponse> ValidateConnection()
    {
        if (!_creds.Get(CredNames.BaseUrl).Value.EndsWith("/anthropic"))
        {
            return new()
            {
                IsValid = false,
                Message = "The endpoint URL must end with '/anthropic'",
            };
        }

        var request = new RestRequest("/models", Method.Get);
        var response = await ExecuteAsync(request);

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized)
        {
            return new()
            {
                IsValid = false,
                Message = response.ErrorMessage,
            };
        }

        return new() { IsValid = true };
    }
}
