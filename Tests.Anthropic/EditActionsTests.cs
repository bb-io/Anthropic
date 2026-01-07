using Tests.Anthropic.Base;
using Apps.Anthropic.Actions;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Anthropic;

[TestClass]
public class EditActionsTests : TestBaseMultipleConnections
{
    [TestMethod, ContextDataSource]
    public async Task EditContent_WithTranslatedXliff_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var editActions = new EditActions(context, FileManager);

        // Act
        var result = await editActions.EditContent(
            new EditContentRequest
            {
                File = new FileReference
                {
                    Name = "contentful.html.xlf",
                    ContentType = "application/xliff+xml"
                },
                Model = "claude-sonnet-4-20250514",
                SourceLanguage = "en",
                TargetLanguage = "fr",
                AdditionalInstructions = "Improve fluency and ensure natural expression while maintaining accuracy"
            });

        // Assert
        TestContext.WriteLine($"Total segments reviewed: {result.TotalSegmentsReviewed}");
        TestContext.WriteLine($"Total segments updated: {result.TotalSegmentsUpdated}");
        PrintResult(result);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
        Assert.IsGreaterThanOrEqualTo(0, result.TotalSegmentsReviewed);
        Assert.IsNotNull(result.Usage);
    }

    [TestMethod, ContextDataSource]
    public async Task EditText_WithTranslatedText_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var editActions = new EditActions(context, FileManager);

        // Act
        var result = await editActions.EditText(
            new EditTextRequest
            {
                SourceText = "Hello, how are you today? I hope you're having a wonderful day!",
                TargetText = "Hola, ¿cómo estás hoy? ¡Espero que tengas un día maravilloso!",
                SourceLanguage = "en",
                TargetLanguage = "es",
                TargetAudience = "Professional business context",
                Model = "claude-sonnet-4-20250514",
                AdditionalInstructions = "Ensure the tone is formal and professional"
            });

        // Assert
        TestContext.WriteLine($"Edited text: {result.EditedText}");
        TestContext.WriteLine($"System prompt: {result.SystemPrompt}");
        TestContext.WriteLine($"User prompt: {result.UserPrompt}");
        PrintResult(result);

        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.EditedText));
        Assert.IsFalse(string.IsNullOrEmpty(result.SystemPrompt));
        Assert.IsFalse(string.IsNullOrEmpty(result.UserPrompt));
        Assert.IsNotNull(result.Usage);
    }
}
