using System.Net;
using System.Security.Cryptography;
using System.Text;
using Apps.Anthropic.Api;
using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Api.Vertex;
using Apps.Anthropic.Connection;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Extensions;
using Apps.Anthropic.Models.Dto;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Tests.Anthropic;

[TestClass]
public class GoogleVertexRestClientTests
{
    [TestMethod]
    public void ConnectionDefinition_ContainsGoogleVertexFields()
    {
        var definition = new ConnectionDefinition();

        var group = definition.ConnectionPropertyGroups.Single(x => x.Name == ConnectionTypes.GoogleVertex);
        var properties = group.ConnectionProperties.ToDictionary(x => x.Name);

        Assert.IsTrue(properties.ContainsKey(CredNames.ProjectId));
        Assert.IsTrue(properties.ContainsKey(CredNames.Location));
        Assert.IsTrue(properties.ContainsKey(CredNames.ServiceAccountJson));
        Assert.IsTrue(properties[CredNames.ServiceAccountJson].Sensitive);
    }

    [TestMethod]
    public void ClientFactory_CreatesVertexClientWithoutUnsupportedCapabilities()
    {
        var client = ClientFactory.Create(CreateCredentials());

        Assert.IsInstanceOfType<GoogleVertexRestClient>(client);
        Assert.IsFalse(client is ISupportsBatching);
        Assert.IsFalse(client is ISupportsSkills);
    }

    [TestMethod]
    [DataRow("global", "https://aiplatform.googleapis.com")]
    [DataRow("us", "https://aiplatform.us.rep.googleapis.com")]
    [DataRow("eu", "https://aiplatform.eu.rep.googleapis.com")]
    [DataRow("us-east5", "https://us-east5-aiplatform.googleapis.com")]
    public void GetApiBaseUrl_ReturnsLocationSpecificHost(string location, string expected)
    {
        Assert.AreEqual(expected, GoogleVertexRestClient.GetApiBaseUrl(location));
    }

    [TestMethod]
    public void BuildMessageUrl_UsesVertexRawPredictEndpoint()
    {
        var client = new GoogleVertexRestClient("my-project", "eu", new StubTokenProvider());

        var result = client.BuildMessageUrl("claude-haiku-4-5@20251001");

        Assert.AreEqual(
            "https://aiplatform.eu.rep.googleapis.com/v1/projects/my-project/locations/eu/" +
            "publishers/anthropic/models/claude-haiku-4-5%4020251001:rawPredict",
            result);
    }

    [TestMethod]
    public void BuildMessagePayload_UsesVertexShapeAndOmitsModel()
    {
        var request = new MessageRequest
        {
            Model = "claude-sonnet-4-6",
            MaxTokens = 1000,
            System = "System prompt",
            StopSequences = ["STOP"],
            Temperature = 0.2f,
            TopP = 0.8f,
            TopK = 10,
            Messages = [new() { Role = "user", Content = "Hello" }]
        };

        var payload = GoogleVertexRestClient.BuildMessagePayload(request);
        var json = JObject.Parse(JsonConvert.SerializeObject(payload));

        Assert.AreEqual("vertex-2023-10-16", json["anthropic_version"]?.Value<string>());
        Assert.AreEqual(1000, json["max_tokens"]?.Value<int>());
        Assert.AreEqual("System prompt", json["system"]?.Value<string>());
        Assert.AreEqual("Hello", json["messages"]?[0]?["content"]?.Value<string>());
        Assert.AreEqual(0.2f, json["temperature"]?.Value<float>());
        Assert.AreEqual(0.8f, json["top_p"]?.Value<float>());
        Assert.AreEqual(10, json["top_k"]?.Value<int>());
        Assert.IsNull(json["model"]);
    }

    [TestMethod]
    public void BuildMessagePayload_FormatsPdfInput()
    {
        var request = new MessageRequest
        {
            Model = "claude-sonnet-4-6",
            MaxTokens = 100,
            System = string.Empty,
            StopSequences = [],
            Messages = [new() { Role = "user", Content = "Read this" }],
            FileData = new InputFileData([1, 2, 3], "test.pdf", ".pdf")
        };

        var payload = GoogleVertexRestClient.BuildMessagePayload(request);
        var json = JObject.Parse(JsonConvert.SerializeObject(payload));
        var content = (JArray)json["messages"]![0]!["content"]!;

        Assert.AreEqual("document", content[0]?["type"]?.Value<string>());
        Assert.AreEqual("application/pdf", content[0]?["source"]?["media_type"]?.Value<string>());
        Assert.AreEqual(Convert.ToBase64String([1, 2, 3]), content[0]?["source"]?["data"]?.Value<string>());
        Assert.AreEqual("Read this", content[1]?["text"]?.Value<string>());
    }

    [TestMethod]
    public void ServiceAccountAssertion_IsValidSignedJwt()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();
        var credential = GoogleServiceAccountCredential.Parse(JsonConvert.SerializeObject(new
        {
            client_email = "vertex-test@example.iam.gserviceaccount.com",
            private_key = privateKey
        }));
        var issuedAt = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

        var assertion = credential.CreateAssertion(issuedAt);
        var parts = assertion.Split('.');
        var payload = JObject.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[1])));

        Assert.HasCount(3, parts);
        Assert.AreEqual("vertex-test@example.iam.gserviceaccount.com", payload["iss"]?.Value<string>());
        Assert.AreEqual(GoogleServiceAccountCredential.CloudPlatformScope, payload["scope"]?.Value<string>());
        Assert.AreEqual(GoogleServiceAccountCredential.TokenUri, payload["aud"]?.Value<string>());
        Assert.AreEqual(issuedAt.ToUnixTimeSeconds(), payload["iat"]?.Value<long>());
        Assert.AreEqual(issuedAt.AddHours(1).ToUnixTimeSeconds(), payload["exp"]?.Value<long>());
        Assert.IsTrue(rsa.VerifyData(
            Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}"),
            Base64UrlDecode(parts[2]),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1));
    }

    [TestMethod]
    public async Task AccessTokenProvider_ReusesTokenOnlyWithinProviderLifetime()
    {
        using var rsa = RSA.Create(2048);
        var credential = GoogleServiceAccountCredential.Parse(JsonConvert.SerializeObject(new
        {
            client_email = "vertex-test@example.iam.gserviceaccount.com",
            private_key = rsa.ExportPkcs8PrivateKeyPem()
        }));
        var handler = new TokenEndpointHandler();
        var restClient = new RestClient(new HttpClient(handler));
        var provider = new GoogleAccessTokenProvider(
            credential,
            restClient,
            () => new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));

        var first = await provider.GetAccessTokenAsync();
        var second = await provider.GetAccessTokenAsync();
        provider.Invalidate();
        var third = await provider.GetAccessTokenAsync();

        Assert.AreEqual("token-1", first);
        Assert.AreEqual("token-1", second);
        Assert.AreEqual("token-2", third);
        Assert.AreEqual(2, handler.RequestCount);
        StringAssert.Contains(handler.LastRequestBody, "grant_type=");
        StringAssert.Contains(handler.LastRequestBody, "assertion=");
    }

    [TestMethod]
    public async Task ListModels_ReturnsVertexModelIds()
    {
        var client = new GoogleVertexRestClient("my-project", "global", new StubTokenProvider());

        var models = await client.ListModels();

        Assert.IsNotEmpty(models);
        Assert.IsTrue(models.Any(x => x.Id == "claude-sonnet-4-6"));
        Assert.IsTrue(models.Any(x => x.Id == "claude-haiku-4-5@20251001"));
        Assert.IsTrue(models.All(x => !string.IsNullOrWhiteSpace(x.DisplayName)));
    }

    [TestMethod]
    public void ModelValidation_RequiresModelForVertex()
    {
        var model = new ModelIdentifier();
        var credentials = new[]
        {
            new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.GoogleVertex)
        };

        Assert.ThrowsExactly<PluginMisconfigurationException>(() => model.Validate(credentials));
    }

    private static IEnumerable<AuthenticationCredentialsProvider> CreateCredentials()
    {
        using var rsa = RSA.Create(2048);
        var serviceAccountJson = JsonConvert.SerializeObject(new
        {
            client_email = "vertex-test@example.iam.gserviceaccount.com",
            private_key = rsa.ExportPkcs8PrivateKeyPem()
        });

        return
        [
            new(CredNames.ConnectionType, ConnectionTypes.GoogleVertex),
            new(CredNames.ProjectId, "my-project"),
            new(CredNames.Location, "global"),
            new(CredNames.ServiceAccountJson, serviceAccountJson)
        ];
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        return Convert.FromBase64String(base64);
    }

    private sealed class StubTokenProvider : IGoogleAccessTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("test-token");

        public void Invalidate()
        {
        }
    }

    private sealed class TokenEndpointHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    access_token = $"token-{RequestCount}",
                    expires_in = 3600,
                    token_type = "Bearer"
                }))
            };
        }
    }
}
