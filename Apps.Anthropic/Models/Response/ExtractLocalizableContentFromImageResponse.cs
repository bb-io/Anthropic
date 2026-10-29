using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Response;

public class ExtractLocalizableContentFromImageResponse
{
    [Display("File")]
    public FileReference File { get; set; } = new();

    [Display("Source language")]
    public string? SourceLanguage { get; set; }

    [Display("Extracted segments count")]
    public int SegmentsCount { get; set; }

    [Display("Usage")]
    public UsageResponse Usage { get; set; } = new();
}
