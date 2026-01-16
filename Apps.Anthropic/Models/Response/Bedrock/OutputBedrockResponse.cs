using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response.Bedrock;

public class OutputBedrockResponse
{
    [JsonProperty("message")]
    public MessageBedrockResponse Message { get; set; }
}
