namespace Apps.Anthropic.Constants;

public static class KnownErrors
{
    public static readonly Dictionary<string, string> AnthropicErrors = new()
    {
        { "authentication_error", "There's an issue with your API key. Check if your key is valid or has expired." },
        { "permission_error", "Your API key does not have permission to use the specified resource." },
        { "not_found_error", "The requested resource was not found." },
        { "request_too_large", "Request exceeds the maximum allowed number of bytes." },
        { "rate_limit_error", "Your account has hit a rate limit." },
        { "api_error", "An unexpected error occurred internally to Anthropic’s systems." },
        { "overloaded_error", "Anthropic’s API is temporarily overloaded. Please retry after some time." },
        { "invalid_request_error", "The provided request is invalid." }
    };
}
