using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Models.Response.Bedrock;
using Apps.Anthropic.Utils;
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

        var formattedMessages = new List<object>();         
        foreach (var m in message.Messages)
        {
            if (m.Role == "user" && message.FileData != null)
            {
                var contentList = new List<object>();

                string base64Data = Convert.ToBase64String(message.FileData.FileBytes);
                string format = message.FileData.FileExtension.TrimStart('.').ToLowerInvariant();
                string name = Path.GetFileNameWithoutExtension(message.FileData.FileName);

                if (format == "pdf")
                {
                    contentList.Add(new
                    {
                        document = new
                        {
                            name,
                            format,
                            source = new { bytes = base64Data }
                        }
                    });
                }
                else if (FileFormatHelper.IsImage(format))
                {
                    if (format == "jpg") 
                        format = "jpeg";

                    contentList.Add(new
                    {
                        image = new
                        {
                            format,
                            source = new { bytes = base64Data }
                        }
                    });
                }
                else
                {
                    throw new PluginMisconfigurationException(
                        $"The file format '{format}' is not supported. Only .pdf and image files are currently allowed"
                    );
                }

                if (!string.IsNullOrEmpty(m.Content))
                    contentList.Add(new { text = m.Content });

                formattedMessages.Add(new { role = m.Role, content = contentList });
                message.FileData = null;
            }
            else
            {
                formattedMessages.Add(new
                {
                    role = m.Role,
                    content = new[] { new { text = m.Content } }
                });
            }
        }

        var payload = new
        {
            messages = formattedMessages,
            system = !string.IsNullOrEmpty(message.System)
                ? new[] { new { text = message.System } }
                : null,
            inferenceConfig = new
            {
                maxTokens = message.MaxTokens,
                temperature = message.Temperature ?? 0.5f,
              //  topP = message.TopP ?? 1f
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
        var request = new RestRequest(url, Method.Get)
            .AddQueryParameter("byProvider", "anthropic")
            .AddQueryParameter("byInferenceType", "ON_DEMAND");

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
        if (response.Content != null)
        {
            var error = JsonConvert.DeserializeObject<RestErrorBedrockResponse>(response.Content);
            return new PluginApplicationException(error?.Message ?? "Error - could not parse the response");
        }

        return new PluginApplicationException("Error - could not parse the response");
    }
}
