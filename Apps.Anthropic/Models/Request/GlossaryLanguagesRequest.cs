using Apps.Anthropic.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Request;

public class GlossaryLanguagesRequest
{
    [DataSource(typeof(GlossaryLanguagesDataHandler))]
    [Display("Languages to use from glossary")]
    public List<string>? LanguagesToFilter { get; set; }

}