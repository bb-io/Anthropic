using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class AcceptNumberRequest
    {
        [Display("Number")]
        public double InputNumber { get; set; }
    }
}
