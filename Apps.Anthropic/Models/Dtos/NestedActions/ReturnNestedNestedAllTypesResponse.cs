using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.NestedActions
{
    public class ReturnNestedNestedAllTypesResponse
    {
        [Display("Nested Object")]
        public NestedObject1 NestedObject1 { get; set; }
    }

    public class NestedObject1
    {
        [Display("Nested Object In Nested Object")]
        public NestedObject NestedObject2 { get; set; }
    }
}
