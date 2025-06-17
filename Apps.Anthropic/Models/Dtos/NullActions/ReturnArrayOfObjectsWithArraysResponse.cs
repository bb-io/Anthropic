using Blackbird.Applications.Sdk.Common;
using TestPlugin.Dtos.ArrayActions;

namespace TestPlugin.Dtos.NullActions
{
    public class ReturnArrayOfObjectsWithArraysResponse
    {
        [Display("Arrays (Array Of Objects)")]
        public List<InputMultipleItemsResponse> ObjectsWithArrays {  get; set; }
    }
}
