using Tests.Anthropic.Base;
using Apps.Anthropic.Actions;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Anthropic;

[TestClass]
public class ChatActionsTests : TestBaseMultipleConnections
{
    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task CreateCompletion_ReturnsValidChatResponse(InvocationContext context)
    {
		// Arrange
		var actions = new ChatActions(context, FileManager);
        var completionRequest = new CompletionRequest 
        {
            Prompt = "Hello, please state your model and your creator",
            Model = "claude-haiku-4-5-20251001"
        };
        var glossary = new GlossaryRequest { };

        // Act
        var result = await actions.CreateCompletion(completionRequest, glossary);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result.Text);
    }
}
