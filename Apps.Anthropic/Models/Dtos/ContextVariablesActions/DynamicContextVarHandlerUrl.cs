using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.ContextVariablesActions
{
    public class DynamicContextVarHandlerUrl
    {
        [Display("Url", Description = "To this url invocation context from data source handler will be send")]
        public string Url { get; set; }
    }
}
