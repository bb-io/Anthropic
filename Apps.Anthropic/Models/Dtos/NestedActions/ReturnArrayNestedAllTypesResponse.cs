using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace TestPlugin.Dtos.NestedActions
{
    public class ReturnArrayNestedAllTypesResponse
    {
        [Display("Nested Objects In Array")]
        public IEnumerable<NestedObjectArrayItem> NestedObjects { get; set; }
    }

    public class NestedObjectArrayItem
    {
        [Display("Nested Text (Item In Array)")]
        public string NestedString { get; set; }

        [Display("Nested Number (Item In Array)")]
        public int NestedNumber { get; set; }

        [Display("Nested Boolean (Item In Array)")]
        public bool NestedBoolean { get; set; }

        [Display("Nested Date (Item In Array)")]
        public DateTime NestedDate { get; set; }

        [Display("Nested File (Item In Array)")]
        public FileReference NestedFile { get; set; }

        [Display("Nested Array With All Types (Array Of Objects Item)")]
        public IEnumerable<ArrayItemNestedInArray> NestedArray { get; set; }
    }

    public class ArrayItemNestedInArray
    {
        public ArrayItemNestedInArray(string arrString, int arrNumber, bool arrBoolean, DateTime arrDate)
        {
            NestedArrayItemString = arrString;
            NestedArrayItemNumber = arrNumber;
            NestedArrayItemBoolean = arrBoolean;
            NestedArrayItemDate = arrDate;
        }

        [Display("Nested Text (Nested Array Item)")]
        public string NestedArrayItemString { get; set; }

        [Display("Nested Number (Nested Array Item)")]
        public int NestedArrayItemNumber { get; set; }

        [Display("Nested Boolean (Nested Array Item)")]
        public bool NestedArrayItemBoolean { get; set; }

        [Display("Nested Date (Nested Array Item)")]
        public DateTime NestedArrayItemDate { get; set; }
    }
}
