using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Anthropic.Actions;

[ActionList("Chat")]
public class ChatActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AnthropicInvocable(invocationContext, fileManagementClient)
{
    [Action("Chat", Description = "Gives a response given a chat message")]
    public async Task<ResponseMessage> CreateCompletion([ActionParameter] CompletionRequest input,
        [ActionParameter] GlossaryRequest glossaryInput)
    {
        var aiUtilities = new AiUtilities(invocationContext, fileManagementClient);
        return await aiUtilities.SendMessageAsync(input, glossaryInput);
    }
}