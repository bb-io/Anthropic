using Apps.Anthropic.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;

namespace Apps.Anthropic.Api;

public static class ClientFactory
{
    public static IAnthropicClient Create(IEnumerable<AuthenticationCredentialsProvider> creds)
    {
        string connectionType = creds.Get(CredNames.ConnectionType).Value;
        return connectionType switch
        {
            ConnectionTypes.AnthropicNative => new AnthropicRestClient(creds),
            ConnectionTypes.BedrockCreds => new AmazonBedrockSdkClient(creds),
            ConnectionTypes.BedrockApiKey => new AmazonBedrockRestClient(creds),
            _ => throw new Exception($"Unsupported connection type: {connectionType}")
        };
    }
}
