using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Response;

public class BackgroundContentResponse
{
    public FileReference File { get; set; } = new();

    public UsageResponse Usage { get; set; } = new();

    [Display("Total segments")]
    public int TotalSegmentsCount { get; set; }

    [Display("Processed segments")]
    public int ProcessedSegmentsCount { get; set; }

    [Display("Updated segments")]
    public int UpdatedSegmentsCount { get; set; }

    [Display("Unresolved segments", Description = "Segments the batch returned no usable result for. They were left unchanged; the reasons are in the logs.")]
    public int UnresolvedSegmentsCount { get; set; }

    [Display("Batch status")]
    public string BatchStatus { get; set; } = string.Empty;

    [Display("Average score", Description = "Average quality score from 0 to 1. Only set for files started with 'Review in background'.")]
    public float? AverageScore { get; set; }
}
