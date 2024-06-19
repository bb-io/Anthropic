using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Response;

public class ScoreXliffResponse
{
    [Display("XLIFF File")]
    public FileReference XliffFile { get; set; }

    [Display("Average Score")]
    public double AverageScore { get; set; }
}