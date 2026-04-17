using Apps.Anthropic.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Anthropic.Models.Identifiers;

public class ModelIdentifier
{
    [Display("Model", 
        Description = "Optional for the 'MS Foundry' connection type. Required to set for all other connection types")]
    [DataSource(typeof(ModelDataSource))]
    public string? Model { get; set; }
}
