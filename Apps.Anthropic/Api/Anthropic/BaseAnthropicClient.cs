using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Constants;
using System.Net;
using Apps.Anthropic.Extensions;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using Polly;
using RestSharp;

namespace Apps.Anthropic.Api.Anthropic;

public class BaseAnthropicClient : BlackBirdRestClient
{
    private static readonly ResiliencePipeline<RestResponse> RateLimitPipeline =
        AnthropicPollyPolicies.CreateRateLimitPipeline();

    protected override JsonSerializerSettings JsonSettings => new() 
    { 
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public BaseAnthropicClient(IEnumerable<AuthenticationCredentialsProvider> creds, Uri baseUrl) :
        base(new RestClientOptions
        {
            ThrowOnAnyError = false,
            BaseUrl = baseUrl,
            MaxTimeout = (int)TimeSpan.FromMinutes(10).TotalMilliseconds,
            
        })
    {
        this.AddDefaultHeader("x-api-key", creds.First(x => x.KeyName == "apiKey").Value);
        this.AddDefaultHeader("anthropic-version", "2023-06-01");
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        if (response.Content == null)
            throw new PluginApplicationException(response.ErrorMessage);

        var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content, JsonSettings);

        if (error?.Error == null || string.IsNullOrWhiteSpace(error.Error.Type))
            throw new PluginApplicationException(error?.Error?.Message ?? response.ErrorException?.Message);

        var errorType = error.Error.Type;

        if (KnownErrors.AnthropicErrors.TryGetValue(errorType, out var message))
        {
            return new PluginApplicationException(error?.Error?.Message ?? message);
        }

        // We should explicitly throw errors here to be notified of invalid request errors that we can fix
        return new Exception(error?.Error?.Message ?? response.ErrorException.Message);
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        RestResponse response;
        try
        {
            response = await RateLimitPipeline.ExecuteAsync(
                cancellationToken => new ValueTask<RestResponse>(ExecuteAsync(request, cancellationToken)));
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new PluginApplicationException(
                "Anthropic rate limit was exceeded after multiple retry attempts. Please try again later.",
                exception);
        }

        return response.IsSuccessStatusCode
            ? response
            : throw ConfigureErrorException(response);
    }

    public virtual async Task<ResponseMessage> ExecuteChat(MessageRequest message)
    {
        var formattedMessages = new List<object>();

        foreach (var msg in message.Messages)
        {
            if (msg.Role == "user" && message.FileData != null)
            {
                var contentList = new List<object>();

                string base64Data = Convert.ToBase64String(message.FileData.FileBytes);
                string ext = message.FileData.FileExtension;

                if (FileFormatHelper.IsPdf(ext))
                {
                    contentList.Add(new
                    {
                        type = "document",
                        source = new
                        {
                            data = base64Data,
                            media_type = "application/pdf",
                            type = "base64",
                        }
                    });
                }
                else if (FileFormatHelper.IsImage(ext))
                {
                    contentList.Add(new
                    {
                        type = "image",
                        source = new
                        {
                            type = "base64",
                            media_type = FileFormatHelper.GetAnthropicImageMediaType(ext),
                            data = base64Data
                        }
                    });
                }
                else
                    throw new PluginMisconfigurationException($"The file format '{ext}' is not supported");

                if (!string.IsNullOrEmpty(msg.Content))
                    contentList.Add(new { type = "text", text = msg.Content });

                formattedMessages.Add(new { role = "user", content = contentList });

                message.FileData = null;
            }
            else
                formattedMessages.Add(new { role = msg.Role, content = msg.Content });
        }

        var payload = BuildPayload(message, formattedMessages);
        
        if (this is ISupportsSkills && !string.IsNullOrEmpty(message.SkillId))
        {
            payload["container"] = new
            {
                skills = new[]
                {
                    new
                    {
                        type = message.SkillId.StartsWith("skill_") ? "custom" : "anthropic",
                        skill_id = message.SkillId,
                        version = "latest"
                    }
                }
            };

            payload["tools"] = new[] 
            {
                new
                {
                    type = "code_execution_20250825", 
                    name = "code_execution"
                }
            };
        }

        if (ModelCatalog.SupportsSamplingParameters(message.Model))
        {
            if (message.Temperature.HasValue)
            {
                payload["temperature"] = message.Temperature.Value;
            }

            if (message.TopP.HasValue)
            {
                payload["top_p"] = message.TopP.Value;
            }

            if (message.TopK.HasValue)
            {
                payload["top_k"] = message.TopK.Value;
            }
        }

        var request = new RestRequest("/messages", Method.Post).WithJsonBody(payload, JsonOptions.JsonSettings);

        if (!string.IsNullOrEmpty(message.SkillId))
            request.AddHeader("anthropic-beta", "code-execution-2025-08-25,skills-2025-10-02");
        
        var response = await ExecuteWithErrorHandling<CompletionResponse>(request);
        return new ResponseMessage
        {
            Text = response.Content.ExtractText(),
            Usage = response.Usage
        };
    }

    internal static Dictionary<string, object?> BuildPayload(MessageRequest message, List<object> formattedMessages)
    {
        var payload = new Dictionary<string, object?>
        {
            ["model"] = message.Model,
            ["max_tokens"] = message.MaxTokens,
            ["messages"] = formattedMessages
        };

        if (!string.IsNullOrWhiteSpace(message.System))
        {
            payload["system"] = message.System;
        }

        if (message.StopSequences is { Count: > 0 })
        {
            payload["stop_sequences"] = message.StopSequences;
        }

        return payload;
    }
}
