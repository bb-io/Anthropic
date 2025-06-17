using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class AcceptDecimlaNumberRequest
    {
        [Display("Decimal Number")]
        public decimal DecimalNumberInput { get; set; }
    }
}
