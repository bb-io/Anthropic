using System.Net;
using System.Text.RegularExpressions;
using Apps.Anthropic.Api.Anthropic;
using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Extensions;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Models.Response.Vertex;
using Apps.Anthropic.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Newtonsoft.Json;
using Polly;
using RestSharp;

namespace Apps.Anthropic.Api.Vertex;

public sealed class GoogleVertexRestClient : RestClient, IAnthropicClient
{
    private const string VertexAnthropicVersion = "vertex-2023-10-16";
    private static readonly Regex LocationPattern = new("^[a-z0-9-]+$", RegexOptions.Compiled);
    private static readonly ResiliencePipeline<RestResponse> RateLimitPipeline =
        AnthropicPollyPolicies.CreateRateLimitPipeline();

    private readonly string _projectId;
    private readonly string _location;
    private readonly string _apiBaseUrl;
    private readonly IGoogleAccessTokenProvider _tokenProvider;

    public GoogleVertexRestClient(IEnumerable<AuthenticationCredentialsProvider> creds)
        : this(CreateSettings(creds))
    {
    }

    private GoogleVertexRestClient(GoogleVertexSettings settings)
        : this(
            settings.ProjectId,
            settings.Location,
            new GoogleAccessTokenProvider(settings.Credential))
    {
    }

    internal GoogleVertexRestClient(
        string projectId,
        string location,
        IGoogleAccessTokenProvider tokenProvider)
        : base(new RestClientOptions
        {
            ThrowOnAnyError = false,
            MaxTimeout = (int)TimeSpan.FromMinutes(10).TotalMilliseconds
        })
    {
        _projectId = RequireValue(projectId, "project ID");
        _location = ValidateLocation(location);
        _apiBaseUrl = GetApiBaseUrl(_location);
        _tokenProvider = tokenProvider;
    }

    public async Task<ConnectionValidationResponse> ValidateConnection()
    {
        try
        {
            var response = await ExecuteAuthorizedAsync(() =>
                new RestRequest(
                    $"{_apiBaseUrl}/v1beta1/publishers/anthropic/models",
                    Method.Get)
                    .AddQueryParameter("pageSize", 1));

            if (!response.IsSuccessStatusCode)
            {
                throw ConfigureErrorException(response);
            }

            return new() { IsValid = true };
        }
        catch (Exception exception)
        {
            return new()
            {
                IsValid = false,
                Message = exception.Message
            };
        }
    }

    public async Task<ResponseMessage> ExecuteChat(MessageRequest message)
    {
        if (string.IsNullOrWhiteSpace(message.Model))
        {
            throw new PluginMisconfigurationException("Please specify the model in the input");
        }

        var payload = BuildMessagePayload(message);
        var url = BuildMessageUrl(message.Model);

        var response = await ExecuteAuthorizedAsync(() =>
            new RestRequest(url, Method.Post).WithJsonBody(payload, JsonOptions.JsonSettings));

        if (!response.IsSuccessStatusCode)
        {
            throw ConfigureErrorException(response);
        }

        CompletionResponse? completion;
        try
        {
            completion = JsonConvert.DeserializeObject<CompletionResponse>(
                response.Content ?? string.Empty,
                JsonOptions.JsonSettings);
        }
        catch (JsonException exception)
        {
            throw new PluginApplicationException("Could not parse the response from Google Vertex AI", exception);
        }

        if (completion == null)
        {
            throw new PluginApplicationException("Google Vertex AI returned an empty response");
        }

        return new()
        {
            Text = completion.Content.ExtractText(),
            Usage = completion.Usage
        };
    }

    public Task<List<ModelResponse>> ListModels() =>
        Task.FromResult(VertexModelCatalog.Models
            .Select(model => new ModelResponse(model.Id, model.DisplayName))
            .ToList());

    internal static string GetApiBaseUrl(string location) => location switch
    {
        "global" => "https://aiplatform.googleapis.com",
        "us" or "eu" => $"https://aiplatform.{location}.rep.googleapis.com",
        _ => $"https://{location}-aiplatform.googleapis.com"
    };

    internal string BuildMessageUrl(string model) =>
        $"{_apiBaseUrl}/v1/projects/{Uri.EscapeDataString(_projectId)}" +
        $"/locations/{Uri.EscapeDataString(_location)}" +
        $"/publishers/anthropic/models/{Uri.EscapeDataString(model)}:rawPredict";

    internal static Dictionary<string, object?> BuildMessagePayload(MessageRequest message)
    {
        var formattedMessages = new List<object>();
        var fileData = message.FileData;

        foreach (var currentMessage in message.Messages)
        {
            if (currentMessage.Role == "user" && fileData != null)
            {
                var content = new List<object>();
                var base64Data = Convert.ToBase64String(fileData.FileBytes);
                var extension = fileData.FileExtension;

                if (FileFormatHelper.IsPdf(extension))
                {
                    content.Add(new
                    {
                        type = "document",
                        source = new
                        {
                            data = base64Data,
                            media_type = "application/pdf",
                            type = "base64"
                        }
                    });
                }
                else if (FileFormatHelper.IsImage(extension))
                {
                    content.Add(new
                    {
                        type = "image",
                        source = new
                        {
                            type = "base64",
                            media_type = FileFormatHelper.GetAnthropicImageMediaType(extension),
                            data = base64Data
                        }
                    });
                }
                else
                {
                    throw new PluginMisconfigurationException(
                        $"The file format '{extension}' is not supported");
                }

                if (!string.IsNullOrEmpty(currentMessage.Content))
                {
                    content.Add(new { type = "text", text = currentMessage.Content });
                }

                formattedMessages.Add(new { role = "user", content });
                fileData = null;
            }
            else
            {
                formattedMessages.Add(new { role = currentMessage.Role, content = currentMessage.Content });
            }
        }

        var payload = new Dictionary<string, object?>
        {
            ["anthropic_version"] = VertexAnthropicVersion,
            ["max_tokens"] = message.MaxTokens,
            ["system"] = message.System,
            ["messages"] = formattedMessages,
            ["stop_sequences"] = message.StopSequences
        };

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

        return payload;
    }

    private async Task<RestResponse> ExecuteAuthorizedAsync(Func<RestRequest> requestFactory)
    {
        var response = await ExecuteWithTokenAsync(requestFactory);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        _tokenProvider.Invalidate();
        return await ExecuteWithTokenAsync(requestFactory);
    }

    private async Task<RestResponse> ExecuteWithTokenAsync(Func<RestRequest> requestFactory)
    {
        var accessToken = await _tokenProvider.GetAccessTokenAsync();
        var request = requestFactory()
            .AddHeader("Authorization", $"Bearer {accessToken}")
            .AddHeader("x-goog-user-project", _projectId);

        try
        {
            return await RateLimitPipeline.ExecuteAsync(
                cancellationToken => new ValueTask<RestResponse>(ExecuteAsync(request, cancellationToken)));
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new PluginApplicationException(
                "Google Vertex AI rate limit was exceeded after multiple retry attempts. Please try again later.",
                exception);
        }
    }

    private static Exception ConfigureErrorException(RestResponse response)
    {
        GoogleVertexErrorResponse? googleError = null;
        CompletionResponse? anthropicError = null;

        if (!string.IsNullOrWhiteSpace(response.Content))
        {
            try
            {
                googleError = JsonConvert.DeserializeObject<GoogleVertexErrorResponse>(response.Content);
                anthropicError = JsonConvert.DeserializeObject<CompletionResponse>(response.Content);
            }
            catch (JsonException)
            {
                // Fall back to the HTTP error below.
            }
        }

        var message = googleError?.Error?.Message
                      ?? anthropicError?.Error?.Message
                      ?? response.ErrorMessage
                      ?? $"Google Vertex AI request failed with status {(int)response.StatusCode}";
        return new PluginApplicationException(message);
    }

    private static GoogleVertexSettings CreateSettings(
        IEnumerable<AuthenticationCredentialsProvider> creds)
    {
        var credentialList = creds.ToList();
        var projectId = credentialList.Get(CredNames.ProjectId).Value;
        var location = credentialList.Get(CredNames.Location).Value;
        var serviceAccountJson = credentialList.Get(CredNames.ServiceAccountJson).Value;

        return new(
            RequireValue(projectId, "project ID"),
            ValidateLocation(location),
            GoogleServiceAccountCredential.Parse(serviceAccountJson));
    }

    private static string ValidateLocation(string value)
    {
        var location = RequireValue(value, "location").ToLowerInvariant();
        if (!LocationPattern.IsMatch(location))
        {
            throw new PluginMisconfigurationException(
                "The Google Vertex AI location may contain only lowercase letters, numbers, and hyphens");
        }

        return location;
    }

    private static string RequireValue(string? value, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PluginMisconfigurationException($"Please specify the {displayName} in the connection");
        }

        return value.Trim();
    }

    private sealed record GoogleVertexSettings(
        string ProjectId,
        string Location,
        GoogleServiceAccountCredential Credential);
}
