using Apps.Anthropic.Api;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using RestSharp;

namespace Apps.Anthropic.Actions;

[ActionList]
public class CompletionActions : BaseInvocable
{
    public CompletionActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }
    
    [Action("Create completion", Description = "Create completion")]
    public async Task<CompletionResponse> CreateCompletion([ActionParameter] CompletionRequest input)
    {
        var client = new AnthropicRestClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new RestRequest("/complete", Method.Post);
        input.Prompt = $"\"\\n\\nHuman: {input.Prompt} \\n\\nAssistant:\"";
        request.AddJsonBody(input);
        return client.Execute<CompletionResponse>(request);
    }    
}