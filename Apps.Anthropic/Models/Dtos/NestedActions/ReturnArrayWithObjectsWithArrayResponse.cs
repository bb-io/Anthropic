using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.NestedActions
{
    public class ReturnArrayWithObjectsWithArrayResponse
    {
        public List<ReturnArrayWithObjectsInner> Array { get; set; }
    }

    public class ReturnArrayWithObjectsInner
    {
        [Display("Inner objects")]
        public List<InnerObject> InnerObjects { get; set; }
    }

    public class InnerObject
    {
        [Display("Text (Inner object)")]
        public string Value { get; set; }
    }
}
