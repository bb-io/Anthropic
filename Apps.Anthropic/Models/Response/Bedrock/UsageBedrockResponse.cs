using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response.Bedrock;

public class UsageBedrockResponse
{
    [JsonProperty("inputTokens")]
    public int InputTokens { get; set; }

    [JsonProperty("outputTokens")]
    public int OutputTokens { get; set; }

    [JsonProperty("cacheReadInputTokens")]
    public int CacheReadInputTokens { get; set; }

    [JsonProperty("cacheWriteInputTokens")]
    public int CacheWriteInputTokens { get; set; }
}
