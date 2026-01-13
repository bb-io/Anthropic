using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response.Bedrock;

public class MessageBedrockResponse
{
    [JsonProperty("content")]
    public List<ContentBedrockResponse> Content { get; set; }
}
