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

    [TestMethod, ContextDataSource(ConnectionTypes.MicrosoftFoundryApiKey)]
    public async Task CreateCompletion_Bedrock_ReturnsValidChatResponse(InvocationContext context)
    {
		// Arrange
		var actions = new ChatActions(context, FileManager);
        var modelId = new ModelIdentifier {  };
        var completionRequest = new CompletionRequest 
        {
            Prompt = "Hello, please state your model and your creator",
            MaxTokensToSample = 128000
        };
        var glossary = new GlossaryRequest { };

        // Act
        var result = await actions.CreateCompletion(modelId, completionRequest, glossary);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result.Text);
    }
}
