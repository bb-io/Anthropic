using Blackbird.Applications.Sdk.Common.Dictionaries;
using TestPlugin.DynamicHandlers.StaticDataHandlers;

namespace TestPlugin.Dtos.StaticDynamicInputActions;

public class StaticDynamicOptionalArrayRequest
{
    [StaticDataSource(typeof(SimpleStaticDataHandler))]
    public IEnumerable<string>? StaticDynamicInput { get; set; }
}