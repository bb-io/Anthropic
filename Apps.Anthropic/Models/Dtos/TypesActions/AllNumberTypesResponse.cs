using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class AllNumberTypesResponse
    {
        [Display("Int Number")]
        public int IntNumber { get; set; }

        [Display("Float Number")]
        public float FloatNumber { get; set; }

        [Display("Decimal Number")]
        public decimal DecimalNumber { get; set; }

        [Display("Double Number")]
        public double DoubleNumber { get; set; }
    }
}
