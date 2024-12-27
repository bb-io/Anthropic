using Blackbird.Applications.Sdk.Common;

namespace Apps.Anthropic.Models.Identifiers;

public class BatchIdentifier
{
    [Display("Batch ID")]
    public string BatchId { get; set; } = string.Empty;
}