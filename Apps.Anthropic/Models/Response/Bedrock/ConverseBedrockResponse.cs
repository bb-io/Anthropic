using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response.Bedrock;

public class ConverseBedrockResponse
{
    [JsonProperty("output")]
    public OutputBedrockResponse Output { get; set; }

    [JsonProperty("usage")]
    public UsageBedrockResponse Usage { get; set; }
}
