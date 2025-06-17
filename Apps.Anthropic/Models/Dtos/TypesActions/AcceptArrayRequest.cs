using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class AcceptArrayRequest
    {
        [Display("Multiple Texts")]
        public IEnumerable<string> InputArrayStrings { get; set; }
    }
}
