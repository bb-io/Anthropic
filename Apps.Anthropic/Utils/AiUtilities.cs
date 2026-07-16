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
    
    public async Task<string> IdentifySourceLanguageAsync(string? model, string content)
    {
        var systemPrompt =
            "You are a linguist. Identify the language of the following text. " +
            "Your response should be in the BCP 47 (language) or (language-country). " +
            "You respond with the language only, not other text is required.";

        var snippet = content.Length > 200 ? content.Substring(0, Math.Min(300, content.Length)) : content;
        var userPrompt = snippet + ". The BCP 47 language code: ";

        var requestBody = new MessageRequest
        {
            System = systemPrompt,
            Model = model,
            Messages = [new() { Role = "user", Content = userPrompt }],
            MaxTokens = 100
        };

        var response = await Client.ExecuteChat(requestBody);
        return response.Text;
    }
    
    public async Task<string?> GetGlossaryPromptPart(FileReference? glossary, string text, bool includeReverse = false)
    {
        if (glossary == null)
        {
            return null;
        }
        
        return await GlossaryPromptHelper.GetGlossaryPromptPart(new GlossaryRequest { Glossary = glossary }, fileManagementClient);
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
