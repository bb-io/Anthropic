﻿using Apps.Anthropic.Extensions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Anthropic.DataSourceHandlers.EnumHandlers;

public class TopPDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public TopPDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        return DataSourceHandlersExtensions.GenerateFormattedFloatArray(0.0f, 1.0f, 0.1f)
            .Where(p => context.SearchString == null || p.Contains(context.SearchString))
            .ToDictionary(p => p, p => p);
    }
}