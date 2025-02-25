using Apps.Anthropic.Api;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common;
using RestSharp;
using Blackbird.Applications.Sdk.Common.Invocation;
using Apps.Anthropic.Models.Request;

namespace Apps.Anthropic.DataSourceHandlers;
public class ModelDataSource(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var client = new AnthropicRestClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new RestRequest("/models");

        var batches = await client.ExecuteWithErrorHandling<DataResponse<ModelResponse>>(request);
        return batches.Data
            .Where(model => context.SearchString == null || model.DisplayName.Contains(context.SearchString))
            .Select(model => new DataSourceItem(model.Id, model.DisplayName));
    }
}