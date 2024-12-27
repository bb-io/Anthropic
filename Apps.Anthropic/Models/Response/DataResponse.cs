using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response;

public class DataResponse<T>
{
    [JsonProperty("data")]
    public List<T> Data { get; set; } = new();
}