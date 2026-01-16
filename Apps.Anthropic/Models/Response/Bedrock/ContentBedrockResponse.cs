using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response.Bedrock;

public class ContentBedrockResponse
{
    [JsonProperty("text")]
    public string Text { get; set; }
}
