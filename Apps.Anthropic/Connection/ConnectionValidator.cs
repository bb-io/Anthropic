using Apps.Anthropic.Api;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using RestSharp;

namespace Apps.Anthropic.Connection;

public class ConnectionValidator : IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authProviders, CancellationToken cancellationToken)
    {
        var client = new AnthropicRestClient(authProviders);
        var request = new RestRequest("/models", Method.Get);

        try
        {
            var response = await client.ExecuteWithErrorHandling(request);
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
}