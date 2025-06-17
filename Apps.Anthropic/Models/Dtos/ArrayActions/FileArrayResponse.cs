using Blackbird.Applications.Sdk.Common.Files;

namespace TestPlugin.Dtos.ArrayActions;

public class FileArrayResponse
{
    public IEnumerable<FileReference> Files { get; set; }
}