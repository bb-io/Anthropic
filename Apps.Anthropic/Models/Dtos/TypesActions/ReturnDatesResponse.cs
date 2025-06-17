using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnDatesResponse
    {
        [Display("Dates (Simple Array)")]
        public IEnumerable<DateTime> Dates { get; set; }
    }
}
