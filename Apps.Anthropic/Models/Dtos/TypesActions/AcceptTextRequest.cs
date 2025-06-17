using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class AcceptTextRequest
    {
        [Display("Text")]
        public string InputText { get; set; }
    }
}
