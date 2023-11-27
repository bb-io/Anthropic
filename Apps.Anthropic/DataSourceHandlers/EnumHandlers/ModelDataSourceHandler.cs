using Apps.Anthropic.Extensions;
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
            { "claude-2", "Claude" },
            { "claude-instant-1", "Claude Instant" },
        };
    }
}
