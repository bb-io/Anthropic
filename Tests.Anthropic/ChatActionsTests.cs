using Tests.Anthropic.Base;
using Apps.Anthropic.Actions;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Anthropic;

[TestClass]
public class ChatActionsTests : TestBaseMultipleConnections
{
    [TestMethod, ContextDataSource(ConnectionTypes.BedrockApiKey)]
    public async Task CreateCompletion_ReturnsValidChatResponse(InvocationContext context)
    {
		// Arrange
		var actions = new ChatActions(context, FileManager);
        var completionRequest = new CompletionRequest 
        {
            Prompt = "Hello, please state your model and your creator",
            Model = "anthropic.claude-3-sonnet-20240229-v1:0"
        };
        var glossary = new GlossaryRequest { };

        // Act
        var result = await actions.CreateCompletion(completionRequest, glossary);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result.Text);
    }
}
