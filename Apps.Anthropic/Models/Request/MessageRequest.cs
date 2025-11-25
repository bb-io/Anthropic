using System.Text.Json.Serialization;

namespace Apps.Anthropic.Models.Request;
public class MessageRequest
{
    public string System { get; set; }
    public string Model { get; set; }
    public List<Message>? Messages { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("stop_sequences")]
    public List<string> StopSequences { get; set; }

    public float? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }

    [JsonPropertyName("top_k")]
    public int? TopK { get; set; }

}
