using Apps.Anthropic.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Anthropic.Models.Identifiers;

public class ModelIdentifier
{
    [Display("Model", Description = "This parameter controls which version of Claude answers your request")]
    [DataSource(typeof(ModelDataSource))]
    public string? Model { get; set; } = string.Empty;
}
