using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using TestPlugin.Dtos.DynamicInputActions;

namespace TestPlugin.DynamicHandlers
{
    public class DynamicSimpleHandler : BaseInvocable, IDataSourceHandler
    {
        public DynamicForContextRequest Input { get; set; }

        public DynamicSimpleHandler(InvocationContext invocationContext, [ActionParameter] DynamicForContextRequest input) : base(invocationContext)
        {
            Input = input;
        }

        public Dictionary<string, string> GetData(DataSourceContext context)
        {
            Dictionary<string, string> dictionary = new() {
                { "Dynamic value1", "Dynamic value1" },
                { "Dynamic value2", "Dynamic value2" },
                { "Dynamic value3", "Dynamic value3" },
            };
            return dictionary.Where(x => string.IsNullOrEmpty(context.SearchString) || x.Key.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase)).ToDictionary(k => k.Key, v => v.Value);
        }
    }
}
