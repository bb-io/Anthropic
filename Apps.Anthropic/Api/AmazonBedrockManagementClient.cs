using Amazon;
using Amazon.Bedrock;
using Apps.Anthropic.Constants;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Apps.Anthropic.Api;

public class AmazonBedrockManagementClient : AmazonBedrockClient
{
    public AmazonBedrockManagementClient(IEnumerable<AuthenticationCredentialsProvider> authProviders)
        : base(
            authProviders.Get(CredNames.AccessKey).Value,
            authProviders.Get(CredNames.SecretKey).Value,
            new AmazonBedrockConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(authProviders.Get(CredNames.Region).Value)
            })
    {
    }


}
