using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnObjFloatsResponse
    {
        [Display("Float Numbers (Array Of Objects)")]
        public IEnumerable<FloatObj> FloatsObjs { get; set; }
    }

    public class FloatObj {

        [Display("Float Number (Array Item)")]
        public float x { get; set; }

    }

}
