using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response.Bedrock;

public class RestErrorBedrockResponse
{
    [JsonProperty("message")]
    public string Message { get; set; }
}
