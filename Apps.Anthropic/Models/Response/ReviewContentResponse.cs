using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Response;

public class ReviewContentResponse : IReviewFileOutput
{
    public FileReference File { get; set; } = new();

    public UsageResponse Usage { get; set; } = new();
    
    public int TotalSegmentsProcessed { get; set; }
    
    public int TotalSegmentsFinalized { get; set; }
    
    public int TotalSegmentsUnderThreshhold { get; set; }
    
    public float AverageMetric { get; set; }
    
    public float PercentageSegmentsUnderThreshhold { get; set; }
}