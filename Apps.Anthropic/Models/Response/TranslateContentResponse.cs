using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Response;

public class TranslateContentResponse : ITranslateFileOutput
{
    public FileReference File { get; set; } = new();

    public UsageResponse Usage { get; set; } = new();
    
    [Display("Total segments count")]
    public double TotalSegmentsCount { get; set; }
    
    [Display("Updated segments count")]
    public double UpdatedSegmentsCount { get; set; } 
}