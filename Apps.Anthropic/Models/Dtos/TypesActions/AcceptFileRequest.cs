using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace TestPlugin.Dtos.TypesActions
{
    public class AcceptFileRequest
    {
        [Display("File")]
        public FileReference File { get; set; }
    }
}
