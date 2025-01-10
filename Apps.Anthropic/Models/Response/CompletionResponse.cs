using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response;

public class CompletionResponse
{
    [JsonProperty("content")]
    public List<Content> Content { get; set; } = new();

    [JsonProperty("stop_reason")]
    public string StopReason { get; set; } = string.Empty;

    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [JsonProperty("usage")]
    public UsageResponse Usage { get; set; } = new();

    [JsonProperty("error")]
    public AnthropicError? Error { get; set; }
}
public class AnthropicError
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}