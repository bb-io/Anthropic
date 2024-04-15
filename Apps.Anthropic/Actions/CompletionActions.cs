using Apps.Anthropic.Api;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Anthropic.Actions;

[ActionList]
public class CompletionActions : BaseInvocable
{
    public CompletionActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }
    
    [Action("Create message", Description = "Create a message")]
    public async Task<ResponseMessage> CreateCompletion([ActionParameter] CompletionRequest input)
    {
        var client = new AnthropicRestClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new RestRequest("/messages", Method.Post);
        var messages = new List<Message>() { new Message { Role = "user", Content = input.Prompt } };
        request.AddJsonBody(new
        {
            system = input.SystemPrompt ?? "",
            model = input.Model,
            messages = messages,
            max_tokens = input.MaxTokensToSample ?? 4096,
            stop_sequences = input.StopSequences != null ? input.StopSequences : new List<string>(),
            temperature = input.Temperature != null ? float.Parse(input.Temperature) : 1.0f,
            top_p = input.TopP != null ? float.Parse(input.TopP) : 1.0f,
            top_k = input.TopK != null ? input.TopK : 1,
        });
        var response = await client.ExecuteWithErrorHandling<CompletionResponse>(request);

        return new ResponseMessage
        {
            Text = response.Content.FirstOrDefault()?.Text ?? ""
        };
    }    
}