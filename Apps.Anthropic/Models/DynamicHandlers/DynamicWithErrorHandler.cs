using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace TestPlugin.DynamicHandlers
{
    public class DynamicWithErrorHandler : BaseInvocable, IDataSourceHandler
    {
        public DynamicWithErrorHandler(InvocationContext invocationContext) : base(invocationContext)
        {
        }

        public Dictionary<string, string> GetData(DataSourceContext context)
        {
            throw new ArgumentException("Something wrong with arguments");
            Dictionary<string, string> dictionary = new() { { "test 1", "1" }, { "test 2", "2" }, { "test 3", "3" } };
            return dictionary.Where(x => x.Value.Contains(context.SearchString)).ToDictionary(k => k.Key, v => v.Value);
        }
    }
}
