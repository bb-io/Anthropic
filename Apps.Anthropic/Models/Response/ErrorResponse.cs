using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response;

public class ErrorResponse
{
    [JsonProperty("error")]
    public Error Error { get; set; }

    public override string ToString() => $"{Error.Type}: {Error.Message}";
}

public class Error
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}