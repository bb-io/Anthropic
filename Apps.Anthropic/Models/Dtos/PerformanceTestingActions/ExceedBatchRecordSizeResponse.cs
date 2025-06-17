using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace TestPlugin.Dtos.PerformanceTestingActions;

public class ExceedBatchRecordSizeResponse
{
    [Display("Exceed batch object")]
    public ExceedBatchObject ExceedBatchObject { get; set; }
}

public class ExceedBatchObject
{
    [Display("Exceed text")]
    public string ExceedString { get; set; }

    [Display("Exceed number")]
    public int ExceedNumber { get; set; }

    [Display("Exceed boolean")]
    public bool ExceedBoolean { get; set; }

    [Display("Exceed date")]
    public DateTime ExceedDate { get; set; }

    [Display("Exceed file")]
    public FileReference ExceedFile { get; set; }

    [Display("Exceed array")]
    public IEnumerable<ExceedBatchArrayItem> ExceedArray { get; set; }
}

public class ExceedBatchArrayItem
{
    public ExceedBatchArrayItem(string str, int number, bool boolean, DateTime date)
    {
        ExceedArrayItemString = str;
        ExceedArrayItemNumber = number;
        ExceedArrayItemBoolean = boolean;
        ExceedArrayItemDate = date;
    }

    [Display("Exceed text (array item)")]
    public string ExceedArrayItemString { get; set; }

    [Display("Exceed number (array item)")]
    public int ExceedArrayItemNumber { get; set; }

    [Display("Exceed boolean (array item)")]
    public bool ExceedArrayItemBoolean { get; set; }

    [Display("Exceed date (array item)")]
    public DateTime ExceedArrayItemDate { get; set; }
}