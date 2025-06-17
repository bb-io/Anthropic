using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace TestPlugin.Dtos.ArrayActions
{
    public class InputMultipleItemsResponse
    {
        [Display("Multiple Texts (Dynamic)")]
        public List<string> OutputDynamicStrings { get; set; }

        [Display("Multiple Texts (Dynamic NH)")]
        public List<string> OutputDynamicStringsNewHandler { get; set; }

        [Display("Multiple Texts (Static)")]
        public List<string> OutputStaticStrings { get; set; }

        [Display("Multiple Texts (Static NH)")]
        public List<string> OutputStaticStringsNewHandler { get; set; }

        [Display("Multiple Texts")]
        public List<string> OutputStrings { get; set; }

        [Display("Multiple Numbers")]
        public List<int> OutputInts { get; set; }

        [Display("Multiple Booleans")]
        public List<bool> OutputBooleans { get; set; }

        [Display("Multiple Dates")]
        public List<DateTime> OutputDateTimes { get; set; }

        [Display("Multiple Files")]
        public List<FileReference> OutputFiles { get; set; }
    }
}
