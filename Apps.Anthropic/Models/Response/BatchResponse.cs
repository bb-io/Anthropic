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

    [Display("Created at"), JsonProperty("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Display("Ended at"), JsonProperty("ended_at")]
    public DateTime? EndedAt { get; set; }

    [Display("Expires at"), JsonProperty("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Display("Cancel initiated at"), JsonProperty("cancel_initiated_at")]
    public DateTime? CancelInitiatedAt { get; set; }
}