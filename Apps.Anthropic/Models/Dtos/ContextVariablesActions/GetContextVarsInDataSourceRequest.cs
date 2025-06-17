using Blackbird.Applications.Sdk.Common.Dynamic;
using TestPlugin.DynamicHandlers;

namespace TestPlugin.Dtos.ContextVariablesActions
{
    public class GetContextVarsInDataSourceRequest
    {
        [DataSource(typeof(DynamicContextVarHandler))]
        public string DataSourceInput {  get; set; }
    }
}
