using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response;

public class CitationResponse
{
    [Display("Title")]
    [JsonProperty("title")]
    public string? Title { get; set; }

    [Display("URL")]
    [JsonProperty("url")]
    public string? Url { get; set; }
}
