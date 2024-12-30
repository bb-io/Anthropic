using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response;

public class UsageResponse
{
    [Display("Input tokens")]
    [JsonProperty("input_tokens")]
    public int InputTokens { get; set; }

    [Display("Cache creation input tokens")]
    [JsonProperty("cache_creation_input_tokens")]
    public int CacheCreationInputTokens { get; set; }

    [Display("Cache read input tokens")]
    [JsonProperty("cache_read_input_tokens")]
    public int CacheReadInputTokens { get; set; }
    
    [Display("Output tokens")]
    [JsonProperty("output_tokens")]
    public int OutputTokens { get; set; }

    public static UsageResponse operator +(UsageResponse u1, UsageResponse u2)
    {
        return new()
        {
            InputTokens = u1.InputTokens + u2.InputTokens,
            CacheCreationInputTokens = u1.CacheCreationInputTokens + u2.CacheCreationInputTokens,
            CacheReadInputTokens = u1.CacheReadInputTokens + u2.CacheReadInputTokens,
            OutputTokens = u1.OutputTokens + u2.OutputTokens
        };
    }
}