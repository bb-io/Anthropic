using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnStringsResponse
    {
        [Display("Texts (Simple Array)")]
        public IEnumerable<string> Strings { get; set; }
    }
}
