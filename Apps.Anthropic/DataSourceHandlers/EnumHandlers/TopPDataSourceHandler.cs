using Apps.Anthropic.Extensions;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Anthropic.DataSourceHandlers.EnumHandlers;

public class TopPDataSourceHandler : IDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData(DataSourceContext context)
    {
        return DataSourceHandlersExtensions.GenerateFormattedFloatArray(0.0f, 1.0f, 0.1f)
            .Select(t => new DataSourceItem(t, t));
    }
}