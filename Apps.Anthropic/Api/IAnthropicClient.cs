using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Apps.Anthropic.Api;

public interface IAnthropicClient
{
    Task<ConnectionValidationResponse> ValidateConnection(IEnumerable<AuthenticationCredentialsProvider> creds);
}
