using Blackbird.Applications.Sdk.Common;

namespace Apps.Anthropic.Models.Response;

public class GetQualityScoreBatchResultResponse : GetBatchResultResponse
{
    [Display("Average score")]
    public double AverageScore { get; set; }
}