using Blackbird.Applications.SDK.Blueprints.Interfaces.Edit;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Response;

public class EditContentResponse : IEditFileOutput
{
    public FileReference File { get; set; } = new();
    
    public UsageResponse Usage { get; set; } = new();
    
    [Display("Total segments reviewed")]
    public int TotalSegmentsReviewed { get; set; }
    
    [Display("Total segments updated")]
    public int TotalSegmentsUpdated { get; set; }
}