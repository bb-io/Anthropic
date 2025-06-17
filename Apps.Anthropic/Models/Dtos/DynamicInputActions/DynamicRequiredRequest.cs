using Blackbird.Applications.Sdk.Common.Dynamic;
using TestPlugin.DynamicHandlers;

namespace TestPlugin.Dtos.DynamicInputActions
{
    public class DynamicRequiredRequest
    {
        [DataSource(typeof(DynamicSimpleHandler))]
        public string DynamicInput { get; set; }

       
    }
}
