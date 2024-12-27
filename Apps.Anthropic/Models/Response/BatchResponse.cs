using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Response;

public class BatchResponse
{
    [Display("Batch ID")]
    public string Id { get; set; } = string.Empty;

    [Display("Batch type")]
    public string Type { get; set; } = string.Empty;
    
    [Display("Processing status"), JsonProperty("processing_status")]
    public string ProcessingStatus { get; set; } = string.Empty;

    [Display("Request counts"), JsonProperty("request_counts")]
    public RequestCountResponse RequestCounts { get; set; } = new();
}