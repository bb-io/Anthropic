using Apps.Anthropic.Api;
using Apps.Anthropic.Api.Interfaces;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Anthropic.Invocable;

public class AnthropicInvocable(InvocationContext invocationContext, IFileManagementClient fileManagementClient) 
    : BaseInvocable(invocationContext)
{
    protected IAnthropicClient Client = ClientFactory.Create(invocationContext.AuthenticationCredentialsProviders);
}