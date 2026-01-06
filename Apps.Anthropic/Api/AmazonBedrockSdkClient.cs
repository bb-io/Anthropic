using Amazon;
using Amazon.Bedrock;
using Amazon.Bedrock.Model;
using Amazon.BedrockRuntime;
using Apps.Anthropic.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;

namespace Apps.Anthropic.Api;

public class AmazonBedrockSdkClient : IAnthropicClient
{
    public readonly AmazonBedrockClient ManagementClient;
    public readonly AmazonBedrockRuntimeClient ChatClient;

    public AmazonBedrockSdkClient(IEnumerable<AuthenticationCredentialsProvider> authProviders)
    {
        var accessKey = authProviders.Get(CredNames.AccessKey).Value;
        var secretKey = authProviders.Get(CredNames.SecretKey).Value;
        var region = RegionEndpoint.GetBySystemName(authProviders.Get(CredNames.Region).Value);

        ManagementClient = new AmazonBedrockClient(accessKey, secretKey, new AmazonBedrockConfig { RegionEndpoint = region });
        ChatClient = new AmazonBedrockRuntimeClient(accessKey, secretKey, new AmazonBedrockRuntimeConfig { RegionEndpoint = region });
    }

    public async Task<ConnectionValidationResponse> ValidateConnection(IEnumerable<AuthenticationCredentialsProvider> creds)
    {
        try
        {
            await ManagementClient.ListFoundationModelsAsync(new ListFoundationModelsRequest());
            return new ConnectionValidationResponse { IsValid = true };
        }
        catch (Exception ex)
        {
            return new ConnectionValidationResponse
            {
                IsValid = false,
                Message = ex.Message
            };
        }
    }
}
