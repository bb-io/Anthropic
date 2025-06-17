using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnObjIntssResponse
    {
        [Display("Numbers (Array Of Objects)")]
        public IEnumerable<IntObj> IntsObjs { get; set; }
    }
    public class IntObj
    {
        [Display("Number (Array Item)")]
        public int x { get; set; }
    }
}
