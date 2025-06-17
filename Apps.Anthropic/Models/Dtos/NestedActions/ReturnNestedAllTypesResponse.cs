using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;


namespace TestPlugin.Dtos.NestedActions
{
    public class ReturnNestedAllTypesResponse
    {
        [Display("Nested Object")]
        public NestedObject NestedObject { get; set; }
    }

    public class NestedObject
    {
        [Display("Nested Text")]
        public string NestedString { get; set; }

        [Display("Nested Number")]
        public int NestedNumber { get; set; }

        [Display("Nested Boolean")]
        public bool NestedBoolean { get; set; }

        [Display("Nested Date")]
        public DateTime NestedDate { get; set; }

        [Display("Nested File")]
        public FileReference NestedFile { get; set; }

        [Display("Nested Array With All Types")]
        public IEnumerable<ArrayItemNested> NestedArray { get; set; }
    }

    public class ArrayItemNested
    {
        public ArrayItemNested(string arrString, int arrNumber, bool arrBoolean, DateTime arrDate)
        {
            NestedArrayItemString = arrString;
            NestedArrayItemNumber = arrNumber;
            NestedArrayItemBoolean = arrBoolean;
            NestedArrayItemDate = arrDate;
        }

        [Display("Nested Text (Array Item)")]
        public string NestedArrayItemString { get; set; }

        [Display("Nested Number (Array Item)")]
        public int NestedArrayItemNumber { get; set; }

        [Display("Nested Boolean (Array Item)")]
        public bool NestedArrayItemBoolean { get; set; }

        [Display("Nested Date (Array Item)")]
        public DateTime NestedArrayItemDate { get; set; }
    }
}
