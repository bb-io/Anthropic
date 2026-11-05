using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Anthropic.Models.Response;

public class Content : ITextContentBlock
{
    public string Type { get; set; }
    public string Text { get; set; }

    [JsonProperty("citations")]
    public List<CitationResponse>? Citations { get; set; }

    [JsonProperty("content")]
    public JToken? ToolResultContent { get; set; }
}
