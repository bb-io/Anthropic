using RestSharp;
using Amazon.Bedrock.Model;
using Apps.Anthropic.Api;
using Apps.Anthropic.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;

namespace Apps.Anthropic.Connection;

public class ConnectionValidator : IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authProviders, CancellationToken cancellationToken)
    {
        if (authProviders.Get(CredNames.ConnectionType).Value == ConnectionTypes.Bedrock)
        {
            var amazonClient = new AmazonBedrockManagementClient(authProviders);
            await amazonClient.ListFoundationModelsAsync(new ListFoundationModelsRequest(), cancellationToken); 
            return new ConnectionValidationResponse { IsValid = true };
        }

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