using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response.Vertex;

public sealed class GoogleVertexErrorResponse
{
    [JsonProperty("error")]
    public GoogleVertexError? Error { get; init; }
}

public sealed class GoogleVertexError
{
    [JsonProperty("message")]
    public string? Message { get; init; }

    [JsonProperty("status")]
    public string? Status { get; init; }
}
