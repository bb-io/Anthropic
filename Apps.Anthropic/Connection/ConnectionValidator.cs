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
        var request = new RestRequest("/complete", Method.Post);
        request.AddJsonBody(new
        {
            model = "claude-2",
            prompt = "\n\nHuman: hello \n\nAssistant:",
            max_tokens_to_sample = 20
        });
        try
        {
            await client.ExecuteWithErrorHandling(request);
            return new()
            {
                IsValid = true
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