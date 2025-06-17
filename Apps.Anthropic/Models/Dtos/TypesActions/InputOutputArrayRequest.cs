using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class InputOutputArrayRequest
    {
        [Display("Multiple Texts Array")]
        public string[] InputStringArray { get; set; }

        [Display("Multiple Numbers Array")]
        public int[] InputIntArray { get; set; }

        [Display("Multiple Booleans Array")]
        public bool[] InputBooleansArray { get; set; }

        [Display("Multiple Dates Array")]
        public DateTime[] InputDateTimeArray { get; set; }
    }
}
