using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnBooleansResponse
    {
        [Display("Booleans (Simple Array)")]
        public IEnumerable<bool> Booleans { get; set; }
    }
}
