﻿using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System.Text;

namespace Apps.Anthropic.Api;

public class AnthropicRestClient : BlackBirdRestClient
{
    public AnthropicRestClient(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders) :
            base(new RestClientOptions() { ThrowOnAnyError = true, BaseUrl = new Uri("https://api.anthropic.com/v1") })
    {
        this.AddDefaultHeader("x-api-key", authenticationCredentialsProviders.First(x => x.KeyName == "apiKey").Value);
        this.AddDefaultHeader("anthropic-version", "2023-06-01");

    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var json = response.Content!;

        var error = JsonConvert.DeserializeObject<ErrorResponse>(json);
        return new(error.Error.ToString());
    }
}