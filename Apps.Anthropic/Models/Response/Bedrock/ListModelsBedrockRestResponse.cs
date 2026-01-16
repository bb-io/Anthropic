using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response.Bedrock;

public class ListModelsBedrockRestResponse
{
    [JsonProperty("modelSummaries")]
    public List<ModelBedrockResponse> Models { get; set; }
}
