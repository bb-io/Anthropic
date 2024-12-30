using Blackbird.Applications.Sdk.Common;

namespace Apps.Anthropic.Models.Request;

public class GetQualityScoreBatchResultRequest : GetBatchResultRequest
{
    [Display("Throw error on any unexpected result")]
    public bool? ThrowExceptionOnAnyUnexpectedResult { get; set; }
}