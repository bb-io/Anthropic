using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnObjectFilesResponse
    {
        [Display("Files (Multiple Items)")]
        public IEnumerable<FileObj> ObjectWithArrayOfFiles { get; set; }
    }

    public class FileObj
    {
        [Display("File (Array Item)")]
        public FileReference File { get; set; }
    }
}
