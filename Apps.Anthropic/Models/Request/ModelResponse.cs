using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Request;

public class ModelResponse(string id, string displayName)
{
    public string Id { get; set; } = id;

    [JsonProperty("display_name")]
    public string DisplayName { get; set; } = displayName;
}
