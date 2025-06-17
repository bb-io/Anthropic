using Blackbird.Applications.Sdk.Common.Files;

namespace TestPlugin.Dtos.ZipFilesActions;

public class FilesResponse
{
    public IEnumerable<FileReference> Files { get; set; }
}