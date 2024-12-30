using Apps.Anthropic.Api;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Anthropic.DataSourceHandlers;

public class BatchDataSource(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var client = new AnthropicRestClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new RestRequest("/messages/batches");
        
        var batches = await client.ExecuteWithErrorHandling<DataResponse<BatchResponse>>(request);
        return batches.Data
            .Where(model => context.SearchString == null || model.Id.Contains(context.SearchString))
            .Where(x => x.ProcessingStatus != "ended")
            .Select(model => new DataSourceItem( model.Id, model.Id));
    }
}