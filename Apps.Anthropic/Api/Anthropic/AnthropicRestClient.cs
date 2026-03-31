using RestSharp;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Apps.Anthropic.Api.Anthropic;

public class AnthropicRestClient(IEnumerable<AuthenticationCredentialsProvider> creds) 
    : BaseAnthropicClient(creds, new Uri("https://api.anthropic.com/v1")), IAnthropicClient
{
    public async Task<ConnectionValidationResponse> ValidateConnection()
    {
        var request = new RestRequest("/models", Method.Get);

        try
        {
            var response = await ExecuteWithErrorHandling(request);
            return new()
            {
                IsValid = response.IsSuccessful,
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

    public async Task<List<ModelResponse>> ListModels()
    {
        var request = new RestRequest("/models");
        var models = await ExecuteWithErrorHandling<DataResponse<ModelResponse>>(request);
        return models.Data.Select(x => new ModelResponse(x.Id, x.DisplayName)).ToList();
    }
}