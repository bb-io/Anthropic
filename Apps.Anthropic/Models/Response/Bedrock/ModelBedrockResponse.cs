using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response.Bedrock;

public class ModelBedrockResponse
{
    [JsonProperty("modelId")]
    public string Id { get; set; }

    [JsonProperty("modelName")]
    public string Name { get; set; }
}
