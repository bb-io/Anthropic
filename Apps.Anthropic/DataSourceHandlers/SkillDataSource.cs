using Apps.Anthropic.Invocable;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Anthropic.DataSourceHandlers;

public class SkillDataSource(InvocationContext context) : AnthropicInvocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var skills = await Client.ListSkills();
        return skills
            .Where(x => string.IsNullOrEmpty(context.SearchString) || x.Name.Contains(context.SearchString))
            .Select(x => new DataSourceItem(x.Id, x.Name));
    }
}