using Apps.Anthropic.Api;
using Apps.Anthropic.Api.Interfaces;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Anthropic.Invocable;

public class AnthropicInvocable(InvocationContext invocationContext) : BaseInvocable(invocationContext)
{
    protected IAnthropicClient Client = ClientFactory.Create(invocationContext.AuthenticationCredentialsProviders);
}