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
                    Name = "Boost the output and quality of your existing localization tools-en-it-TRA.mxliff",
                },
                Glossary= new FileReference
                {
                    Name = "Localization Term Base (MT).tbx",
                },
                Model = "claude-sonnet-4-5-20250929",
                SourceLanguage = "en",
                TargetLanguage = "it",
                AdditionalInstructions = "Use \"Lei\" to address the reader. Make sure terms are translated consistently.  "
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
