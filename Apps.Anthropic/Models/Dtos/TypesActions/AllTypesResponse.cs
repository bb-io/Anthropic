using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using TestPlugin.Dtos.NestedActions;

namespace TestPlugin.Dtos.TypesActions
{
    public class AllTypesResponse
    {
        [Display("Text")]
        public string String { get; set; }

        [Display("Number")]
        public double Number { get; set; }

        [Display("Boolean")]
        public bool Boolean { get; set; }

        [Display("Date")]
        public DateTime Date { get; set; }

        [Display("File")]
        public FileReference File { get; set; }

        [Display("Array With All Types")]
        public IEnumerable<ArrayItem> Array { get; set; }

        [DefinitionIgnore]
        public string IgnoredField { get; set; }
    }

    public class ArrayItem
    {
        public ArrayItem(string arrString, int arrNumber, bool arrBoolean, DateTime arrDate)
        {
            ArrayItemString = arrString;
            ArrayItemNumber = arrNumber;
            ArrayItemBoolean = arrBoolean;
            ArrayItemDate = arrDate;
            IgnoredField = "Ignore content";
        }

        [Display("Text (Array Item)")]
        public string ArrayItemString { get; set; }

        [Display("Number (Array Item)")]
        public int ArrayItemNumber { get; set; }

        [Display("Boolean (Array Item)")]
        public bool ArrayItemBoolean { get; set; }

        [Display("Date (Array Item)")]
        public DateTime ArrayItemDate { get; set; }

        [DefinitionIgnore]
        public string IgnoredField { get; set; }

    }
}
