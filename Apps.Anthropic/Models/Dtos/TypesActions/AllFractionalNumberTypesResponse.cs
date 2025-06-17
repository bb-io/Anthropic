using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class AllFractionalNumberTypesResponse
    {
        [Display("Float Number")]
        public float FloatNumber { get; set; }

        [Display("Decimal Number")]
        public decimal DecimalNumber { get; set; }

        [Display("Double Number")]
        public double DoubleNumber { get; set; }
    }
}
