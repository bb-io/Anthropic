using Apps.Anthropic.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Anthropic.Models.Identifiers;

public class BatchIdentifier
{
    [Display("Batch ID"), DataSource(typeof(BatchDataSource))]
    public string BatchId { get; set; } = string.Empty;
}