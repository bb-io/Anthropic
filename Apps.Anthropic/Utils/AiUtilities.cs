using Apps.Anthropic.Extensions;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Dto;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
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
        var messages = await GenerateChatMessages(input, glossaryRequest);

        InputFileData? fileData = null;
        if (input.File != null)
        {
            var ext = Path.GetExtension(input.File.Name).ToLowerInvariant();
            if (FileFormatHelper.IsSupportedNatively(ext))
                fileData = await ProcessInputFile(input.File);
            else
            {
                var text = await ExtractFileText(input.File);
                if (!string.IsNullOrWhiteSpace(text))
                    messages.Add(new Message { Role = "user", Content = $"File content:\r\n{text}" });
            }
        }
        
        var body = new MessageRequest
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
            SkillId = input.SkillId
        };

        return await Client.ExecuteChat(body);
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
    
    private async Task<List<Message>> GenerateChatMessages(CompletionRequest input, GlossaryRequest glossaryRequest)
    {
        var messages = new List<Message>();

        string prompt = input.Prompt;
        if (glossaryRequest.Glossary != null)
        {
            var glossaryPromptPart = await GlossaryPromptHelper.GetGlossaryPromptPart(glossaryRequest, fileManagementClient);
            prompt += glossaryPromptPart;
        }

        messages.Add(new Message { Role = "user", Content = prompt });
        return messages;
    }
}
