using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Models.Response.Bedrock;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apps.Anthropic.Api;

public class AmazonBedrockRestClient : RestClient, IAnthropicClient
{
    private readonly string _runtimeUrl;
    private readonly string _standardUrl;

    protected static JsonSerializerSettings JsonSettings => new() { MissingMemberHandling = MissingMemberHandling.Ignore };

    public AmazonBedrockRestClient(IEnumerable<AuthenticationCredentialsProvider> creds) :
            base(new RestClientOptions
            {
                ThrowOnAnyError = false,
                MaxTimeout = (int)TimeSpan.FromMinutes(10).TotalMilliseconds,
            }, configureSerialization: s => s.UseSystemTextJson(
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }))
    {
        string apiKey = creds.Get(CredNames.ApiKey).Value;
        string region = creds.Get(CredNames.Region).Value;
        this.AddDefaultHeader("Authorization", $"Bearer {apiKey}"); 
        
        _runtimeUrl = $"https://bedrock-runtime.{region}.amazonaws.com";
        _standardUrl = $"https://bedrock.{region}.amazonaws.com";
    }

    public async Task<ResponseMessage> ExecuteChat(MessageRequest message)
    {
        var runtimeUrl = $"{_runtimeUrl}/model/{message.Model}/converse";
        var restRequest = new RestRequest(runtimeUrl, Method.Post);

        var payload = new
        {
            messages = message.Messages?.Select(m => new
            {
                role = m.Role,
                content = new[] { new { text = m.Content } }
            }),
            system = !string.IsNullOrEmpty(message.System)
                ? new[] { new { text = message.System } }
                : null,
            inferenceConfig = new
            {
                maxTokens = message.MaxTokens,
                temperature = message.Temperature ?? 0.5f,
                topP = message.TopP ?? 1f
            }
        };
        restRequest.AddJsonBody(payload);

        var response = await ExecuteWithErrorHandling<ConverseBedrockResponse>(restRequest);
        return new ResponseMessage
        {
            Text = response.Output.Message.Content[0].Text ?? "",
            Usage = new UsageResponse
            {
                OutputTokens = response.Usage.OutputTokens,
                InputTokens = response.Usage.InputTokens,
                CacheCreationInputTokens = response.Usage.CacheWriteInputTokens,
                CacheReadInputTokens = response.Usage.CacheReadInputTokens,
            }
        };
    }

    public async Task<List<ModelResponse>> ListModels()
    {
        var url = $"{_standardUrl}/foundation-models";
        var request = new RestRequest(url, Method.Get).AddQueryParameter("byProvider", "anthropic");

        var response = await ExecuteWithErrorHandling<ListModelsBedrockRestResponse>(request);
        return response.Models.Select(x => new ModelResponse(x.Id, x.Name)).ToList();
    }

    public async Task<ConnectionValidationResponse> ValidateConnection()
    {
        var url = $"{_standardUrl}/foundation-models";
        var request = new RestRequest(url, Method.Get).AddQueryParameter("byProvider", "anthropic");

        try
        {
            var response = await ExecuteWithErrorHandling(request);
            return new() { IsValid = response.IsSuccessful };
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

    protected static Exception ConfigureErrorException(RestResponse response)
    {
        if (response.Content != null && response.ErrorMessage != null)
            throw new PluginApplicationException(response.ErrorMessage);

        return new PluginApplicationException("Error - could not parse the response");
    }
}
