using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnFloatsResponse
    {
        [Display("Float Numbers (Simple Array)")]
        public IEnumerable<float> FloatNumbers { get; set; }
    }
}
