using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Response;

public class GetBatchResultResponse
{
    public FileReference File { get; set; } = default!;

    public UsageResponse Usage { get; set; } = default!;
}