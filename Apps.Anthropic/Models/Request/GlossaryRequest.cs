using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Request;

public class GlossaryRequest
{
    public FileReference? Glossary { get; set; }

    [Display("Filter glossary", Description = "By default, only glossary entries containing terms found in the prompt or the file text are included. Matching is exact and case-insensitive. Set to false to include the entire glossary.")]
    public bool? FilterGlossary { get; set; }
}
