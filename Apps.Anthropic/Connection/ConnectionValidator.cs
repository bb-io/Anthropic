using Apps.Anthropic.Api;
using Apps.Anthropic.DataSourceHandlers.EnumHandlers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using RestSharp;

namespace Apps.Anthropic.Connection;

public class ConnectionValidator : IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authProviders, CancellationToken cancellationToken)
    {
        var client = new AnthropicRestClient(authProviders);
        var request = new RestRequest("/complete", Method.Post);
        request.AddJsonBody(new CompletionRequest()
        {
            Model = new ModelDataSourceHandler().GetData(new Blackbird.Applications.Sdk.Common.Dynamic.DataSourceContext() { SearchString = string.Empty}).First().Key,
            Prompt = "\"\\n\\nHuman: hello \\n\\nAssistant:\"",
            MaxTokensToSample = 20
        });
        try
        {
            client.Execute(request);
            return new()
            {
                IsValid = true
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }
    }
}