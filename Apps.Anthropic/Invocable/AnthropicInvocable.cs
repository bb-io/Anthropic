using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Xliff.Utils;
using Blackbird.Xliff.Utils.Extensions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Apps.Anthropic.Constants;

namespace Apps.Anthropic.Invocable;

public class AnthropicInvocable(InvocationContext invocationContext, IFileManagementClient fileManagementClient) 
    : BaseInvocable(invocationContext)
{
    protected readonly string ConnectionType =
        invocationContext.AuthenticationCredentialsProviders.Get(CredNames.ConnectionType).Value;

    protected async Task<XliffDocument> LoadAndParseXliffDocument(FileReference inputFile)
    {
        var stream = await fileManagementClient.DownloadAsync(inputFile);
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream.ConvertFromXliff();
    }
}