using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Response;

public class ProcessXliffResponse
{
    [Display("XLIFF file")]
    public FileReference Xliff { get; set; } = new();

    public UsageResponse Usage { get; set; } = new();
    
    [Display("Total segments count")]
    public double TotalSegmentsCount { get; set; }

    [Display("Updated segments count")]
    public double UpdatedSegmentsCount { get; set; } 
}