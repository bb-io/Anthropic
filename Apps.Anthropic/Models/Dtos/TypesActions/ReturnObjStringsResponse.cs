using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnObjStringsResponse
    {
        [Display("Texts (Array Of Objects)")]
        public IEnumerable<StringObj> StringsObjs { get; set; }
    }

    public class StringObj
    {
        [Display("Text (Array Item)")]
        public string x { get; set; }
    }
}
