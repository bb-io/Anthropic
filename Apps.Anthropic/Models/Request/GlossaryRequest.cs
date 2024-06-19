using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Request;

public class GlossaryRequest
{
    public FileReference? Glossary { get; set; }
}