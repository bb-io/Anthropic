using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Identifiers;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;

namespace Apps.Anthropic.Extensions;

public static class ModelIdentifierExtensions
{
    public static void Validate(this ModelIdentifier modelIdentifier, IEnumerable<AuthenticationCredentialsProvider> creds)
    {
        string connectionType = creds.Get(CredNames.ConnectionType).Value;

        switch (connectionType)
        {
            case ConnectionTypes.MicrosoftFoundryApiKey:
                if (string.IsNullOrEmpty(creds.Get(CredNames.DeploymentName).Value))
                    throw new PluginMisconfigurationException("Please specify the deployment name in the connection");

                break;

            case ConnectionTypes.AnthropicNative or ConnectionTypes.BedrockCreds or ConnectionTypes.BedrockApiKey:
                if (string.IsNullOrEmpty(modelIdentifier.Model))
                    throw new PluginMisconfigurationException("Please specify the model in the input");

                break;
        }
    }
}
