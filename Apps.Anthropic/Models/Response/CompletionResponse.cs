using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response;

public class CompletionResponse
{
    [JsonProperty("content")]
    public List<Content> Content { get; set; }

    [JsonProperty("stop_reason")]
    public string StopReason { get; set; }

    [JsonProperty("model")]
    public string Model { get; set; }
}