using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Xliff.Utils;
using Blackbird.Xliff.Utils.Extensions;

namespace Apps.Anthropic.Utils;

public static class FileManagerHelper
{
    public static async Task<XliffDocument> LoadXliffDocument(FileReference inputFile, IFileManagementClient fileClient)
    {
        var stream = await fileClient.DownloadAsync(inputFile);
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream.ConvertFromXliff();
    }
}
