using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Request;

public class BaseXliffRequest
{
    public FileReference File { get; set; } = default!;

    [Display("Max tokens")]
    public int? MaxTokens { get; set; }
    
    public FileReference? Glossary { get; set; }
}