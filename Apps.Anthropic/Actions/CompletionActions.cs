using Apps.Anthropic.Api;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using RestSharp;
using System.Reflection;

namespace Apps.Anthropic.Actions;

[ActionList]
public class CompletionActions : BaseInvocable
{
    public CompletionActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }
    
    [Action("Create message", Description = "Create a message")]
    public async Task<CompletionResponse> CreateCompletion([ActionParameter] CompletionRequest input)
    {
        var client = new AnthropicRestClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new RestRequest("/messages", Method.Post);
        var messages = new List<Message>() { new Message { Role = "user", Content = input.Prompt } };
        request.AddJsonBody(new
        {
            system = input.SystemPrompt,
            model = input.Model,
            messages = messages,
            max_tokens = input.MaxTokensToSample ?? 4096,
            stop_sequences = input.StopSequences != null ? input.StopSequences : new List<string>(),
            temperature = input.Temperature != null ? float.Parse(input.Temperature) : 1.0f,
            top_p = input.TopP != null ? float.Parse(input.TopP) : 1.0f,
            top_k = input.TopK != null ? input.TopK : 1,
        });
        return await client.ExecuteWithErrorHandling<CompletionResponse>(request);
    }    
}