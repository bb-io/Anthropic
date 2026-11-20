using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Response;

public class ShortenContentResponse
{
    public FileReference File { get; set; } = new();

    [Display("Units with length limit")]
    public int UnitsWithLimit { get; set; }

    [Display("Units over length limit")]
    public int UnitsOverLimit { get; set; }

    [Display("Units shortened")]
    public int UnitsShortened { get; set; }

    [Display("Units still over length limit")]
    public int UnitsStillOverLimit { get; set; }

    [Display("Usage")]
    public UsageResponse Usage { get; set; } = new();
}
