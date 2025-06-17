using Blackbird.Applications.Sdk.Common.Dynamic;
using TestPlugin.DynamicHandlers;

namespace TestPlugin.Dtos.DynamicInputActions
{
    public class DynamicOptionalRequest
    {
        [DataSource(typeof(DynamicSimpleHandler))]
        public string? DynamicInput { get; set; }
    }
}
