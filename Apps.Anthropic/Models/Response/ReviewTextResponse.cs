using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Anthropic.Models.Response;

public class ReviewTextResponse : IReviewTextOutput
{
    [Display("Quality score")]
    public float Score { get; set; }

    [Display("System prompt")]
    public string SystemPrompt { get; set; } = string.Empty;
    
    [Display("User prompt")]
    public string UserPrompt { get; set; } = string.Empty;

    public UsageResponse Usage { get; set; } = new();
}
