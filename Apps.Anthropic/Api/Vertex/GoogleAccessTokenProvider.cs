using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Anthropic.Api.Vertex;

internal interface IGoogleAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    void Invalidate();
}

internal sealed class GoogleAccessTokenProvider : IGoogleAccessTokenProvider
{
    private static readonly TimeSpan RefreshBuffer = TimeSpan.FromMinutes(5);

    private readonly GoogleServiceAccountCredential _credential;
    private readonly RestClient _client;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    internal GoogleAccessTokenProvider(GoogleServiceAccountCredential credential)
        : this(
            credential,
            new RestClient(new RestClientOptions
            {
                ThrowOnAnyError = false,
                MaxTimeout = (int)TimeSpan.FromSeconds(30).TotalMilliseconds
            }),
            () => DateTimeOffset.UtcNow)
    {
    }

    internal GoogleAccessTokenProvider(
        GoogleServiceAccountCredential credential,
        RestClient client,
        Func<DateTimeOffset> utcNow)
    {
        _credential = credential;
        _client = client;
        _utcNow = utcNow;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (HasUsableToken())
        {
            return _accessToken!;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (HasUsableToken())
            {
                return _accessToken!;
            }

            var now = _utcNow();
            var request = new RestRequest(GoogleServiceAccountCredential.TokenUri, Method.Post)
                .AddParameter("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer")
                .AddParameter("assertion", _credential.CreateAssertion(now));

            var response = await _client.ExecuteAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(response.Content))
            {
                throw CreateTokenException(response);
            }

            GoogleAccessTokenResponse? token;
            try
            {
                token = JsonConvert.DeserializeObject<GoogleAccessTokenResponse>(response.Content);
            }
            catch (JsonException)
            {
                token = null;
            }

            if (string.IsNullOrWhiteSpace(token?.AccessToken))
            {
                throw new PluginApplicationException("Google OAuth did not return an access token");
            }

            _accessToken = token.AccessToken;
            _expiresAt = now.AddSeconds(Math.Max(token.ExpiresIn, 1));
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public void Invalidate()
    {
        _accessToken = null;
        _expiresAt = default;
    }

    private bool HasUsableToken() =>
        !string.IsNullOrWhiteSpace(_accessToken) && _expiresAt - RefreshBuffer > _utcNow();

    private static Exception CreateTokenException(RestResponse response)
    {
        GoogleOAuthErrorResponse? error = null;
        if (!string.IsNullOrWhiteSpace(response.Content))
        {
            try
            {
                error = JsonConvert.DeserializeObject<GoogleOAuthErrorResponse>(response.Content);
            }
            catch (JsonException)
            {
                // Fall back to the HTTP error below.
            }
        }

        var message = error?.ErrorDescription
                      ?? response.ErrorMessage
                      ?? $"Google OAuth request failed with status {(int)response.StatusCode}";
        return new PluginApplicationException(message);
    }
}

internal sealed class GoogleAccessTokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; init; }
}

internal sealed class GoogleOAuthErrorResponse
{
    [JsonProperty("error_description")]
    public string? ErrorDescription { get; init; }
}
