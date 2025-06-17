using Blackbird.Applications.Sdk.Common.Dynamic;
using TestPlugin.DynamicHandlers;

namespace TestPlugin.Dtos.DynamicInputActions
{
    public class DynamicErrorRequest
    {
        [DataSource(typeof(DynamicWithErrorHandler))]
        public string ErrorInput { get; set; }
    }
}
