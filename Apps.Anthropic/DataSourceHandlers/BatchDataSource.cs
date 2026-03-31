using Apps.Anthropic.Invocable;
using Apps.Anthropic.Api.Interfaces;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Anthropic.DataSourceHandlers;

public class BatchDataSource : AnthropicInvocable, IAsyncDataSourceItemHandler
{
    private readonly ISupportsBatching _batchClient;

    public BatchDataSource(InvocationContext invocationContext) : base(invocationContext)
    {
        if (Client is not ISupportsBatching batchClient)
            throw new PluginMisconfigurationException("This connection type does not support batches");

        _batchClient = batchClient;
    }

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken ct)
    {
        var batches = await _batchClient.ListBatches();
        return batches
            .Where(x => context.SearchString == null || x.Id.Contains(context.SearchString))
            .Where(x => x.ProcessingStatus != "ended")
            .Select(x => new DataSourceItem(x.Id, x.Id));
    }
}