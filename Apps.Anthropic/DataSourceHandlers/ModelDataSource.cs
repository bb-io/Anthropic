using Apps.Anthropic.Invocable;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Anthropic.DataSourceHandlers;

public class ModelDataSource(InvocationContext invocationContext)
    : AnthropicInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var models = await Client.ListModels();
        return models
            .Where(model => context.SearchString == null || model.DisplayName.Contains(context.SearchString))
            .Select(model => new DataSourceItem(model.Id, model.DisplayName));
    }
}