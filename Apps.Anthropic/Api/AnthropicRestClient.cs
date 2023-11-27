using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System.Text;

namespace Apps.Anthropic.Api;

public class AnthropicRestClient : RestClient
{
    public AnthropicRestClient(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders) :
            base(new RestClientOptions() { ThrowOnAnyError = true, BaseUrl = new Uri("https://api.anthropic.com/v1") }, configureSerialization: s => s.UseNewtonsoftJson())
    {
        this.AddDefaultHeader("x-api-key", authenticationCredentialsProviders.First(x => x.KeyName == "apiKey").Value);
        this.AddDefaultHeader("anthropic-version", "2023-06-01");

    }

    public T Get<T>(RestRequest request)
    {
        var resultStr = this.Get(request).Content;
        return JsonConvert.DeserializeObject<T>(resultStr, GetSerializerSettings())!;
    }

    public T Execute<T>(RestRequest request)
    {
        var resultStr = this.Execute(request).Content;
        return JsonConvert.DeserializeObject<T>(resultStr, GetSerializerSettings())!;
    }

    private JsonSerializerSettings GetSerializerSettings()
    {
        var options = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore
        };
        return options;
    }
}