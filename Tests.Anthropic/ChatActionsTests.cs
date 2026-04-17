using Apps.Anthropic.Actions;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.Anthropic.Base;

namespace Tests.Anthropic;

[TestClass]
public class ChatActionsTests : TestBaseMultipleConnections
{
    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task CreateCompletion_Anthropic_ReturnsValidChatResponse(InvocationContext context)
    {
        // Arrange
        var actions = new ChatActions(context, FileManager);
        var modelId = new ModelIdentifier { Model = "claude-sonnet-4-6" };
        var completionRequest = new CompletionRequest
        {
            Prompt = "Please read this PDF file and describe what you see in it",
            File = new FileReference { Name = "test.pdf" },
        };
        var glossary = new GlossaryRequest { };

        // Act
        var result = await actions.CreateCompletion(modelId, completionRequest, glossary);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result.Text);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.BedrockApiKey)]
    public async Task CreateCompletion_Bedrock_ReturnsValidChatResponse(InvocationContext context)
    {
		// Arrange
		var actions = new ChatActions(context, FileManager);
        var modelId = new ModelIdentifier { Model = "anthropic.claude-3-sonnet-20240229-v1:0" };
        var completionRequest = new CompletionRequest 
        {
            Prompt = "Hello, please state your model and your creator",
        };
        var glossary = new GlossaryRequest { };

        // Act
        var result = await actions.CreateCompletion(modelId, completionRequest, glossary);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result.Text);
    }
}
