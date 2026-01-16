using Apps.Anthropic.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Anthropic.DataSourceHandlers;

public class ModelDataSource(InvocationContext invocationContext)
 : BaseInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var client = ClientFactory.Create(InvocationContext.AuthenticationCredentialsProviders);
        var models = await client.ListModels();
        return models
            .Where(model => context.SearchString == null || model.DisplayName.Contains(context.SearchString))
            .Select(model => new DataSourceItem(model.Id, model.DisplayName));
    }
}