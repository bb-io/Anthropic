using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Dto;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

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
            fileData = await ProcessInputFile(input.File);

        var body = new MessageRequest
        {
            System = input.SystemPrompt ?? string.Empty,
            Model = modelIdentifier.Model,
            Messages = messages,
            MaxTokens = input.MaxTokensToSample,
            StopSequences = input.StopSequences != null ? input.StopSequences : new List<string>(),
            Temperature = input.Temperature != null ? float.Parse(input.Temperature) : null,
            TopP = input.TopP != null ? float.Parse(input.TopP) : null,
            TopK = input.TopK,
            FileData = fileData,
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
            MaxTokens = 100,
            TopK = 1
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
        using var fileStream = await fileManagementClient.DownloadAsync(file); 
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);

        string fileName = file.Name ?? "unknown_file";
        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        return new(ms.ToArray(), fileName, extension);
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