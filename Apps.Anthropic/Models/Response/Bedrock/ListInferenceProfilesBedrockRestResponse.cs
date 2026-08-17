using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response.Bedrock;

public class ListInferenceProfilesBedrockRestResponse
{
    [JsonProperty("inferenceProfileSummaries")]
    public List<InferenceProfileBedrockResponse> Profiles { get; set; } = [];

    [JsonProperty("nextToken")]
    public string? NextToken { get; set; }
}

public class InferenceProfileBedrockResponse
{
    [JsonProperty("inferenceProfileId")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("inferenceProfileName")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("models")]
    public List<InferenceProfileModelBedrockResponse> Models { get; set; } = [];
}

public class InferenceProfileModelBedrockResponse
{
    [JsonProperty("modelArn")]
    public string Arn { get; set; } = string.Empty;
}
