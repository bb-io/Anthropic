using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Invocable;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Anthropic.DataSourceHandlers;

public class SkillDataSource : AnthropicInvocable, IAsyncDataSourceItemHandler
{
    private readonly ISupportsSkills _skillsClient;

    public SkillDataSource(InvocationContext context) : base(context)
    {
        if (Client is not ISupportsSkills skillsClient)
            throw new PluginMisconfigurationException("This connection type does not support skills");
            
        _skillsClient = skillsClient;
    }

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var skills = await _skillsClient.ListSkills();
        return skills
            .Where(x => string.IsNullOrEmpty(context.SearchString) || x.Name.Contains(context.SearchString))
            .Select(x => new DataSourceItem(x.Id, x.Name));
    }
}