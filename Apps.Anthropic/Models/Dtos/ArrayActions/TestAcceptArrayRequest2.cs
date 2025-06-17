using Blackbird.Applications.Sdk.Common.Dynamic;
using TestPlugin.DynamicHandlers;

namespace TestPlugin.Dtos.ArrayActions
{
    public class TestAcceptArrayRequest2
    {
        [DataSource(typeof(DynamicTestHandler))]
        public List<string> StringArray2 { get; set; }
    }
}
