using System.Collections.Generic;
using System.Linq;
using Apps.Anthropic.Extensions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Anthropic.DataSourceHandlers;

public class TemperatureDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public TemperatureDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        return DataSourceHandlersExtensions.GenerateFormattedFloatArray(0.0f, 1.0f, 0.1f)
            .Where(t => context.SearchString == null || t.Contains(context.SearchString))
            .ToDictionary(t => t, t => t);
    }
}