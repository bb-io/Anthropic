using Apps.Anthropic.Extensions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Anthropic.DataSourceHandlers.EnumHandlers;

public class TopPDataSourceHandler(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return DataSourceHandlersExtensions.GenerateFormattedFloatArray(0.0f, 1.0f, 0.1f)
            .Select(t => new DataSourceItem(t, t));
    }
}