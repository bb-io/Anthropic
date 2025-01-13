using System.Net;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Anthropic.Api;

public class AnthropicRestClient : BlackBirdRestClient
{
    private readonly Dictionary<string, string> AnthropicErrors = new()
    {
        { "invalid_request_error", "There was an issue with the format or content of your request." },
        { "authentication_error", "There's an issue with your API key. Check if your key is valid or has expired." },
        { "permission_error", "Your API key does not have permission to use the specified resource." },
        { "not_found_error", "The requested resource was not found." },
        { "request_too_large", "Request exceeds the maximum allowed number of bytes." },
        { "rate_limit_error", "Your account has hit a rate limit." },
        { "api_error", "An unexpected error occurred internally to Anthropic’s systems." },
        { "overloaded_error", "Anthropic’s API is temporarily overloaded. Please retry after some time." }

    };
    protected override JsonSerializerSettings JsonSettings =>
           new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore };

    public AnthropicRestClient(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders) :
            base(new RestClientOptions { ThrowOnAnyError = false, BaseUrl = new Uri("https://api.anthropic.com/v1"), MaxTimeout = (int)TimeSpan.FromMinutes(10).TotalMilliseconds })
    {
        this.AddDefaultHeader("x-api-key", authenticationCredentialsProviders.First(x => x.KeyName == "apiKey").Value);
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

        if (AnthropicErrors.TryGetValue(errorType, out var message))
        {
            return errorType switch
            {
                "not_found_error" or "api_error" or "overloaded_error" =>
                     new PluginApplicationException(message),
                _ => new PluginMisconfigurationException(message)
            };
        }

        return new PluginApplicationException(error?.Error?.Message ?? response.ErrorException.Message);
    }
}