using Apps.Anthropic.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Anthropic.Models.Request.Optional;

public class OptionalSkillRequest
{
    [Display("Skill ID"), DataSource(typeof(SkillDataSource))]
    public string? SkillId { get; set; }
}