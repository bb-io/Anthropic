using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.LargeFileActions
{
    public class DownloadFileAndGetRefRequest
    {
        [Display("Url of large file")]
        public string DownloadUrl { get; set; }
        
        public string? Name { get; set; }
        
        public string? ContentType { get; set; }
    }
}
