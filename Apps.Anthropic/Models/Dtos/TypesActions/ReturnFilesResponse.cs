using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace TestPlugin.Dtos.TypesActions
{
    public class ReturnFilesResponse
    {
        [Display("Files (Simple Array)")]
        public IEnumerable<FileReference> Files { get; set; }
    }
}
