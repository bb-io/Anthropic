using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnObjDatesResponse
    {
        [Display("Dates (Array Of Objects)")]
        public IEnumerable<DateObj> DatesObjs { get; set; }
    }

    public class DateObj
    {
        [Display("Date (Array Item)")]
        public DateTime x { get; set; }
    } 
}
