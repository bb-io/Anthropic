using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnIntssResponse
    {
        [Display("Numbers (Simple Array)")]
        public IEnumerable<int> IntNumbers { get; set; }
    }
}
