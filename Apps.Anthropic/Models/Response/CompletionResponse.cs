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
}