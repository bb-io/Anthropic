using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Response;

public class BackgroundProcessingResponse
{
    [Display("Batch ID", Description = "Empty when there was nothing to process and no batch was created.")]
    public string BatchId { get; set; } = string.Empty;

    [Display("Status")]
    public string Status { get; set; } = string.Empty;

    [Display("Created at")]
    public DateTime? CreatedAt { get; set; }

    [Display("Transformation file", Description = "Pass this file to 'Download background file' once the batch has finished.")]
    public FileReference TransformationFile { get; set; } = new();

    [Display("Total segments")]
    public int TotalSegmentsCount { get; set; }

    [Display("Segments to process")]
    public int SegmentsToProcessCount { get; set; }

    [Display("Requests in batch")]
    public int RequestsCount { get; set; }

    [Display("System prompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [Display("Usage", Description = "Tokens used before the batch was submitted: source language detection and the document summary.")]
    public UsageResponse Usage { get; set; } = new();
}
