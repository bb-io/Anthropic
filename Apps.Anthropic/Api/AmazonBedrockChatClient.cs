using Amazon;
using Amazon.BedrockRuntime;
using Apps.Anthropic.Constants;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Apps.Anthropic.Api;

public class AmazonBedrockChatClient : AmazonBedrockRuntimeClient
{
    public AmazonBedrockChatClient(IEnumerable<AuthenticationCredentialsProvider> authProviders)
        : base(
            authProviders.Get(CredNames.AccessKey).Value,
            authProviders.Get(CredNames.SecretKey).Value,
            new AmazonBedrockRuntimeConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(authProviders.Get(CredNames.Region).Value)
            })
    {
    }
}
