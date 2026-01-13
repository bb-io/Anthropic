using Apps.Anthropic.Constants;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Apps.Anthropic.Connection;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            DisplayName = "Anthropic API token",
            Name = ConnectionTypes.AnthropicNative,
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = 
            [
                new(CredNames.ApiKey) { DisplayName = "API token", Sensitive = true }
            ]
        },
        new()
        {
            DisplayName = "Amazon Bedrock (AWS Credentials)",
            Name = ConnectionTypes.BedrockCreds,
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = 
            [
                new(CredNames.AccessKey) { DisplayName = "Access key" },
                new(CredNames.SecretKey) { DisplayName = "Secret key", Sensitive = true },
                new(CredNames.Region) { DisplayName = "Region" },
            ]
        },
        new()
        {
            DisplayName = "Amazon Bedrock (API Key)",
            Name = ConnectionTypes.BedrockApiKey,
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = 
            [
                new(CredNames.ApiKey) { DisplayName = "API token", Sensitive = true },
                new(CredNames.Region) { DisplayName = "Region" }
            ]
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(Dictionary<string, string> values)
    {
        var providers = values.Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value)).ToList();

        var connectionType = values[nameof(ConnectionPropertyGroup)] switch
        {
            var ct when ConnectionTypes.SupportedConnectionTypes.Contains(ct) => ct,
            _ => throw new Exception($"Unknown connection type: {values[nameof(ConnectionPropertyGroup)]}")
        };

        providers.Add(new AuthenticationCredentialsProvider(CredNames.ConnectionType, connectionType));
        return providers;
    }
}