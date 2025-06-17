using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using TestPlugin.Dtos.DynamicInputActions;

namespace TestPlugin.DynamicHandlers;

public class DynamicItemsSimpleHandler : BaseInvocable, IDataSourceItemHandler
{
    public DynamicForContextRequest Input { get; set; }

    public DynamicItemsSimpleHandler(InvocationContext invocationContext, [ActionParameter] DynamicForContextRequest input) :
        base(invocationContext)
    {
        Input = input;
    }

    public IEnumerable<DataSourceItem> GetData(DataSourceContext context)
    {
        Dictionary<string, string> dictionary = new()
        {
            { "Dynamic value1", "Dynamic value1" },
            { "Dynamic value2", "Dynamic value2" },
            { "Dynamic value3", "Dynamic value3" },
        };
        return dictionary
            .Where(x => string.IsNullOrEmpty(context.SearchString) ||
                        x.Key.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(k => new DataSourceItem(k.Key, k.Value));
    }
}