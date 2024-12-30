using System.Text.Json.Serialization;
using Apps.Anthropic.Models.Response;
using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Entities;

public class BatchRequestDto
{
    [JsonProperty("custom_id")]
    public string CustomId { get; set; } = default!;

    [JsonProperty("result")]
    public ResultDto Result { get; set; } = default!;
}

public class ResultDto
{
    [JsonProperty("type")]
    public string Type { get; set; } = default!;

    [JsonProperty("message")]
    public MessageDto Message { get; set; } = default!;
}

public class MessageDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = default!;

    [JsonProperty("type")]
    public string Type { get; set; } = default!;

    [JsonProperty("role")]
    public string Role { get; set; } = default!;

    [JsonProperty("model")]
    public string Model { get; set; } = default!;

    [JsonProperty("content")]
    public List<ContentItemDto> Content { get; set; } = default!;

    [JsonProperty("stop_reason")]
    public string StopReason { get; set; } = default!;

    [JsonProperty("stop_sequence")]
    public object StopSequence { get; set; } = default!;

    [JsonProperty("usage")]
    public UsageResponse Usage { get; set; } = default!;
}

public class ContentItemDto
{
    [JsonProperty("type")]
    public string Type { get; set; } = default!;

    [JsonProperty("text")]
    public string Text { get; set; } = default!;
}
