using System.Globalization;
using System.Net;
using Polly;
using Polly.Retry;
using RestSharp;

namespace Apps.Anthropic.Api.Anthropic;

public static class AnthropicPollyPolicies
{
    internal const int DefaultRetryCount = 5;
    internal static readonly TimeSpan MaximumServerDelay = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan MaximumFallbackDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaximumJitter = TimeSpan.FromMilliseconds(500);

    public static ResiliencePipeline<RestResponse> CreateRateLimitPipeline(int retryCount = DefaultRetryCount)
    {
        var options = new RetryStrategyOptions<RestResponse>
        {
            MaxRetryAttempts = retryCount,
            ShouldHandle = new PredicateBuilder<RestResponse>()
                .HandleResult(ShouldRetry)
                .Handle<HttpRequestException>(exception =>
                    exception.StatusCode == HttpStatusCode.TooManyRequests),
            DelayGenerator = args => new ValueTask<TimeSpan?>(
                GetDelay(args.Outcome.Result, args.AttemptNumber))
        };

        return new ResiliencePipelineBuilder<RestResponse>()
            .AddRetry(options)
            .Build();
    }

    internal static bool ShouldRetry(RestResponse response)
    {
        if (response.StatusCode != HttpStatusCode.TooManyRequests)
        {
            return false;
        }

        return !TryGetServerDelay(response, out var delay) || delay <= MaximumServerDelay;
    }

    internal static bool TryGetServerDelay(RestResponse response, out TimeSpan delay)
    {
        if (TryGetHeaderValue(response, "retry-after-ms", out var retryAfterMilliseconds) &&
            TryParseNonNegativeNumber(retryAfterMilliseconds, out var milliseconds))
        {
            delay = TimeSpan.FromMilliseconds(milliseconds);
            return true;
        }

        if (TryGetHeaderValue(response, "Retry-After", out var retryAfter))
        {
            if (TryParseNonNegativeNumber(retryAfter, out var seconds))
            {
                delay = TimeSpan.FromSeconds(seconds);
                return true;
            }

            if (DateTimeOffset.TryParse(retryAfter, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var retryAt))
            {
                delay = retryAt - DateTimeOffset.UtcNow;
                if (delay < TimeSpan.Zero)
                {
                    delay = TimeSpan.Zero;
                }

                return true;
            }
        }

        delay = default;
        return false;
    }

    internal static TimeSpan GetDelay(RestResponse? response, int attemptNumber)
    {
        if (response is not null && TryGetServerDelay(response, out var serverDelay))
        {
            return serverDelay;
        }

        var exponentialSeconds = Math.Min(
            Math.Pow(2, attemptNumber),
            MaximumFallbackDelay.TotalSeconds);
        var jitterMilliseconds = Random.Shared.NextDouble() * MaximumJitter.TotalMilliseconds;

        return TimeSpan.FromSeconds(exponentialSeconds) + TimeSpan.FromMilliseconds(jitterMilliseconds);
    }

    private static bool TryGetHeaderValue(RestResponse response, string name, out string value)
    {
        value = response.Headers?
            .FirstOrDefault(header => header.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
            ?.Value?.ToString() ?? string.Empty;

        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryParseNonNegativeNumber(string value, out double number) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && number >= 0;
}
