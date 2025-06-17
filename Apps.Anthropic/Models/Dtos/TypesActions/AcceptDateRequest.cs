using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class AcceptDateRequest
    {
        [Display("Date")]
        public DateTime InputDate { get; set; }
    }
}
