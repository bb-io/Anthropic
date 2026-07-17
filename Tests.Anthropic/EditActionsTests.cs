using Tests.Anthropic.Base;
using Apps.Anthropic.Actions;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request.Optional;

namespace Tests.Anthropic;

[TestClass]
public class EditActionsTests : TestBaseMultipleConnections
{
    [TestMethod, ContextDataSource]
    public async Task EditContent_WithTranslatedXliff_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var editActions = new EditActions(context, FileManager);
        var model = new ModelIdentifier { Model = "claude-sonnet-5" };
        var skillRequest = new OptionalSkillRequest { SkillId = "" };

        // Act
        var result = await editActions.EditContent(
            model,
            new EditContentRequest
            {
                File = new FileReference { Name = "test-edit.xlf" },
                SourceLanguage = "en",
                TargetLanguage = "nl",
                ModifiedBy = "Arnold Schwarzenegger"
            },
            skillRequest);

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
        var model = new ModelIdentifier { Model = "claude-sonnet-5" };
        var skillRequest = new OptionalSkillRequest { SkillId = "" };

        // Act
        var result = await editActions.EditText(
            model,
            new EditTextRequest
            {
                SourceText = "Hello, how are you today? I hope you're having a wonderful day!",
                TargetText = "Hola, ¿cómo estás hoy? ¡Espero que tengas un día maravilloso!",
                SourceLanguage = "en",
                TargetLanguage = "es",
                TargetAudience = "Professional business context",
                AdditionalInstructions = "Ensure the tone is formal and professional"
            },
            skillRequest);

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
