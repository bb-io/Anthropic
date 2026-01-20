using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Xliff.Utils;
using Blackbird.Xliff.Utils.Extensions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Apps.Anthropic.Constants;

namespace Apps.Anthropic.Invocable;

public class AnthropicInvocable(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : BaseInvocable(invocationContext)
{
    private static readonly List<string> AcceptedExtensions = [".xlf", ".xliff", ".mqxliff", ".mxliff", ".txlf", ".html"];

    protected readonly string ConnectionType =
        invocationContext.AuthenticationCredentialsProviders.Get(CredNames.ConnectionType).Value;

    protected void ThrowIfXliffInvalid(FileReference xliffFile)
    {
        bool isValidExtension = AcceptedExtensions.Any(ext => xliffFile.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        if (!isValidExtension)
        {
            var expectedExtensions = string.Join(", ", AcceptedExtensions);
            var actualExtension = Path.GetExtension(xliffFile.Name);
            throw new PluginMisconfigurationException($"Invalid file extension '{actualExtension}'. Expected one of: {expectedExtensions}.");
        }
    }
    
    protected async Task<XliffDocument> LoadAndParseXliffDocument(FileReference inputFile)
    {
        var stream = await fileManagementClient.DownloadAsync(inputFile);
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream.ConvertFromXliff();
    }
  
    protected string GetUserPrompt(string prompt, XliffDocument xliffDocument, string json)
    {
        string instruction = string.IsNullOrEmpty(prompt)
            ? $"Translate the following texts from {xliffDocument.SourceLanguage} to {xliffDocument.TargetLanguage}."
            : $"Process the following texts as per the custom instructions: {prompt}. The source language is {xliffDocument.SourceLanguage} and the target language is {xliffDocument.TargetLanguage}. This information might be useful for the custom instructions.";

        return
            $"Please provide a translation for each individual text, even if similar texts have been provided more than once. " +
            $"{instruction} Return the outputs as a serialized JSON array of strings without additional formatting " +
            $"If you see XML tags in the source also include them in the target text, don't delete or modify them. " +
            $"(it is crucial because your response will be deserialized programmatically. Please ensure that your response is formatted correctly to avoid any deserialization issues). " +
            $"Review and edit the translated target text as necessary to ensure it is a correct and accurate translation of the source text. " +
            $"Original texts (in serialized array format): {json}";
    }
}