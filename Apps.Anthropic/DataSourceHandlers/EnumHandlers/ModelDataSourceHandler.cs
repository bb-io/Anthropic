using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Anthropic.DataSourceHandlers.EnumHandlers;

public class ModelDataSourceHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
        => new()
        {
            { "claude-3-5-sonnet-20240620", "Claude 3.5 Sonet" },
            { "claude-3-opus-20240229", "Claude 3 Opus" },
            { "claude-3-sonnet-20240229", "Claude 3 Sonnet" },
            { "claude-2.1", "Claude 2.1" },
            { "claude-2", "Claude 2" },
            { "claude-instant-1", "Claude Instant" },
        };
}