using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnObjectArraysResponse
    {
        [Display("Arrays (Array Of Objects)")]
        public IEnumerable<ArrayObj> Arrays { get; set; }
    }

    public class ArrayObj
    {
        [Display("Texts (Array Of Objects Item)")]
        public IEnumerable<string> ArrayStrings { get; set; }
    }
}
