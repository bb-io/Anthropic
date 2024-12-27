using Blackbird.Applications.Sdk.Common;

namespace Apps.Anthropic.Models.Request;

public class ProcessXliffFileRequest : BaseXliffRequest
{
    [Display("Instructions", Description = "Instructions for processing the XLIFF file. For example, 'Translate the text.")]
    public string? Prompt { get; set; }
}