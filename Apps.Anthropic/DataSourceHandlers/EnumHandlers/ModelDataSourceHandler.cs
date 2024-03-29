﻿using Apps.Anthropic.Extensions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Anthropic.DataSourceHandlers.EnumHandlers
{
    public class ModelDataSourceHandler : EnumDataHandler
    {
        protected override Dictionary<string, string> EnumValues => new Dictionary<string, string>()
        {
            { "claude-3-opus-20240229", "Claude 3 Opus" },
            { "claude-3-sonnet-20240229", "Claude 3 Sonnet" },
            { "claude-2.1", "Claude 2.1" },
            { "claude-2", "Claude 2" },
            { "claude-instant-1", "Claude Instant" },
        };
    }
}
