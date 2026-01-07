using RestSharp;
using RestSharp.Serializers.Json;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Apps.Anthropic.Utils;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Apps.Anthropic.Api;

public class AnthropicRestClient : RestClient, IAnthropicClient
{
    private readonly Dictionary<string, string> AnthropicErrors = new()
    {
        { "authentication_error", "There's an issue with your API key. Check if your key is valid or has expired." },
        { "permission_error", "Your API key does not have permission to use the specified resource." },
        { "not_found_error", "The requested resource was not found." },
        { "request_too_large", "Request exceeds the maximum allowed number of bytes." },
        { "rate_limit_error", "Your account has hit a rate limit." },
        { "api_error", "An unexpected error occurred internally to Anthropic’s systems." },
        { "overloaded_error", "Anthropic’s API is temporarily overloaded. Please retry after some time." }

    };
    protected JsonSerializerSettings JsonSettings =>
           new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore };

    public AnthropicRestClient(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders) :
            base(new RestClientOptions { 
                ThrowOnAnyError = false, 
                BaseUrl = new Uri("https://api.anthropic.com/v1"), 
                MaxTimeout = (int)TimeSpan.FromMinutes(10).TotalMilliseconds,
            }, configureSerialization: s => s.UseSystemTextJson(
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }))
    {
        this.AddDefaultHeader("x-api-key", authenticationCredentialsProviders.First(x => x.KeyName == "apiKey").Value);
        this.AddDefaultHeader("anthropic-version", "2023-06-01");
    }

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

    public async Task<ResponseMessage> ExecuteChat(MessageRequest message)
    {
        message.MaxTokens = ModelTokenService.GetMaxTokensForModel(message.Model);

        var request = new RestRequest("/messages", Method.Post);
        request.AddJsonBody(message);

        var response = await ExecuteWithErrorHandling<CompletionResponse>(request);
        return new ResponseMessage
        {
            Text = response.Content.FirstOrDefault()?.Text ?? "",
            Usage = response.Usage
        };
    }

    protected Exception ConfigureErrorException(RestResponse response)
    {
        if (response.Content == null)
            throw new PluginApplicationException(response.ErrorMessage);

        var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content, JsonSettings);

        if (error?.Error == null || string.IsNullOrWhiteSpace(error.Error.Type))
            throw new PluginApplicationException(error?.Error?.Message ?? response.ErrorException?.Message);

        var errorType = error.Error.Type;

        if (AnthropicErrors.TryGetValue(errorType, out var message))
        {
            return new PluginApplicationException(error?.Error?.Message ?? message);
        }

        // We should explicitly throw errors here to be notified of invalid request errors that we can fix
        return new Exception(error?.Error?.Message ?? response.ErrorException.Message);
    }

    public async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        string content = (await ExecuteWithErrorHandling(request)).Content;
        T val = JsonConvert.DeserializeObject<T>(content, JsonSettings);
        if (val == null)
        {
            throw new Exception($"Could not parse {content} to {typeof(T)}");
        }

        return val;
    }

    public async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        RestResponse restResponse = await ExecuteAsync(request);
        if (!restResponse.IsSuccessStatusCode)
        {
            throw ConfigureErrorException(restResponse);
        }

        return restResponse;
    }
}