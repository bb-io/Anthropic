using Blackbird.Applications.Sdk.Common.Dictionaries;
using TestPlugin.DynamicHandlers.StaticDataHandlers;

namespace TestPlugin.Dtos.StaticDynamicInputActions;

public class StaticDynamicRequiredRequest
{
    [StaticDataSource(typeof(SimpleStaticDataHandler))]
    public string StaticDynamicInput { get; set; }
}