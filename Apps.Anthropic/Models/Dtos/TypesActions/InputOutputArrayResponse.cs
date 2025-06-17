using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class InputOutputArrayResponse
    {
        [Display("Multiple Texts Array")]
        public string[] OutputStringArray { get; set; }

        [Display("Multiple Numbers Array")]
        public int[] OutputIntArray { get; set; }

        [Display("Multiple Booleans Array")]
        public bool[] OutputBooleansArray { get; set; }

        [Display("Multiple Dates Array")]
        public DateTime[] OutputDateTimeArray { get; set; }
    }
}
