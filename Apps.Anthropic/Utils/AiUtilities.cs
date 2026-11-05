using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Extensions;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Dto;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;

namespace Apps.Anthropic.Utils;

public class AiUtilities(InvocationContext invocationContext, IFileManagementClient fileManagementClient) 
    : AnthropicInvocable(invocationContext)
{
    public async Task<ResponseMessage> SendMessageAsync(
        ModelIdentifier modelIdentifier, 
        CompletionRequest input, 
        GlossaryRequest glossaryRequest)
    {
        var body = await BuildMessageRequest(modelIdentifier, input, glossaryRequest);
        return await Client.ExecuteChat(body);
    }

    internal async Task<MessageRequest> BuildMessageRequest(
        ModelIdentifier modelIdentifier,
        CompletionRequest input,
        GlossaryRequest glossaryRequest)
    {
        var webSearchTool = WebSearchToolBuilder.Build(input);
        if (webSearchTool != null && Client is not ISupportsWebSearch)
        {
            throw new PluginMisconfigurationException(
                "Web search is not supported for Amazon Bedrock connections. Please use an Anthropic API, Microsoft Foundry or Google Vertex AI connection.");
        }

        InputFileData? fileData = null;
        string? fileText = null;
        if (input.File != null)
        {
            var ext = Path.GetExtension(input.File.Name).ToLowerInvariant();
            if (FileFormatHelper.IsSupportedNatively(ext))
                fileData = await ProcessInputFile(input.File);
            else
                fileText = await ExtractFileText(input.File);
        }

        var messages = await GenerateChatMessages(input, glossaryRequest, fileText);

        return new MessageRequest
        {
            System = input.SystemPrompt ?? string.Empty,
            Model = modelIdentifier.Model,
            Messages = messages,
            MaxTokens = input.MaxTokensToSample ?? ModelCatalog.GetMaxOutputTokens(modelIdentifier.Model),
            StopSequences = input.StopSequences != null ? input.StopSequences : new List<string>(),
            Temperature = input.Temperature.ToOptionalFloat("temperature"),
            TopP = input.TopP.ToOptionalFloat("top_p"),
            TopK = input.TopK,
            FileData = fileData,
            SkillId = input.SkillId,
            WebSearchTool = webSearchTool
        };
    }

    private async Task<InputFileData> ProcessInputFile(FileReference file)
    {
        await using var fileStream = await fileManagementClient.DownloadAsync(file);
        var fileBytes = await fileStream.GetByteData();

        string fileName = file.Name ?? "unknown_file";
        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        return new(fileBytes, fileName, extension);
    }
    
    private async Task<string> ExtractFileText(FileReference file)
    {
        await using var fileStream = await fileManagementClient.DownloadAsync(file);
        var content = fileStream.LoadTransformation(file.Name);

        return content.ExtractText();
    }
    
    internal async Task<List<Message>> GenerateChatMessages(
        CompletionRequest input,
        GlossaryRequest glossaryRequest,
        string? fileText = null)
    {
        var prompt = input.Prompt;
        if (glossaryRequest.Glossary != null)
        {
            prompt += await GlossaryPromptHelper.GetGlossaryPromptPart(
                glossaryRequest,
                fileManagementClient,
                [input.Prompt, fileText]);
        }

        var messages = new List<Message> { new() { Role = "user", Content = prompt } };

        if (!string.IsNullOrWhiteSpace(fileText))
            messages.Add(new Message { Role = "user", Content = $"File content:\r\n{fileText}" });

        return messages;
    }
}
