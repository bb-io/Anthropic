using System.Security.Cryptography;
using System.Text;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json;

namespace Apps.Anthropic.Api.Vertex;

internal sealed class GoogleServiceAccountCredential
{
    internal const string TokenUri = "https://oauth2.googleapis.com/token";
    internal const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

    [JsonProperty("client_email")]
    public string ClientEmail { get; init; } = string.Empty;

    [JsonProperty("private_key")]
    public string PrivateKey { get; init; } = string.Empty;

    internal static GoogleServiceAccountCredential Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new PluginMisconfigurationException("Please specify the service account JSON in the connection");
        }

        GoogleServiceAccountCredential? credential;
        try
        {
            credential = JsonConvert.DeserializeObject<GoogleServiceAccountCredential>(json);
        }
        catch (JsonException)
        {
            throw new PluginMisconfigurationException("The service account JSON is not valid JSON");
        }

        if (string.IsNullOrWhiteSpace(credential?.ClientEmail))
        {
            throw new PluginMisconfigurationException("The service account JSON must contain 'client_email'");
        }

        if (string.IsNullOrWhiteSpace(credential.PrivateKey))
        {
            throw new PluginMisconfigurationException("The service account JSON must contain 'private_key'");
        }

        return credential;
    }

    internal string CreateAssertion(DateTimeOffset issuedAt)
    {
        var header = JsonConvert.SerializeObject(new { alg = "RS256", typ = "JWT" });
        var payload = JsonConvert.SerializeObject(new
        {
            iss = ClientEmail,
            scope = CloudPlatformScope,
            aud = TokenUri,
            iat = issuedAt.ToUnixTimeSeconds(),
            exp = issuedAt.AddHours(1).ToUnixTimeSeconds()
        });

        var unsignedToken = $"{Base64UrlEncode(header)}.{Base64UrlEncode(payload)}";

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(PrivateKey);
            var signature = rsa.SignData(
                Encoding.UTF8.GetBytes(unsignedToken),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return $"{unsignedToken}.{Base64UrlEncode(signature)}";
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            throw new PluginMisconfigurationException(
                "The 'private_key' in the service account JSON is not a valid RSA private key");
        }
    }

    private static string Base64UrlEncode(string value) =>
        Base64UrlEncode(Encoding.UTF8.GetBytes(value));

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
