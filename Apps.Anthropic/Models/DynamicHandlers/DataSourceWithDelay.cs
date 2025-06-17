using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using TestPlugin.Dtos.PerformanceTestingActions;

namespace TestPlugin.DynamicHandlers;

public class DataSourceWithDelay : BaseInvocable, IAsyncDataSourceHandler
{
    private readonly EmulateActivityRequest _request;

    public DataSourceWithDelay(InvocationContext invocationContext, [ActionParameter] EmulateActivityRequest request) :
        base(invocationContext)
    {
        _request = request;
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        await Task.Delay(_request.TimeoutSeconds is 0 ? 30000 : _request.TimeoutSeconds * 1000, cancellationToken);

        Dictionary<string, string> dictionary = new()
        {
            { "Dynamic value1", "Dynamic value1" },
            { "Dynamic value2", "Dynamic value2" },
            { "Dynamic value3", "Dynamic value3" },
        };
        return dictionary
            .Where(x => string.IsNullOrEmpty(context.SearchString) ||
                        x.Key.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(k => k.Key, v => v.Value);
    }
}