using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnObjectBooleansResponse
    {
        [Display("Booleans (Array Of Objects)")]
        public IEnumerable<BooleanObj> BooleansObjs { get; set; }
    }

    public class BooleanObj
    {
        [Display("Boolean (Array Item)")]
        public bool x { get; set; }
    }
}
