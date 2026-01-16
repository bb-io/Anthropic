using Tests.Anthropic.Base;
using Apps.Anthropic.Actions;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Anthropic;

[TestClass]
public class TranslationActionsTests : TestBaseMultipleConnections
{
    [TestMethod, ContextDataSource]
    public async Task Translate_WithSimpleXliff_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var translationActions = new TranslationActions(context, FileManager);

        // Act
        var result = await translationActions.Translate(
            new TranslateContentRequest
            {
                File = new FileReference
                {
                    Name = "contentful.html",
                    ContentType = "application/xliff+xml"
                },
                Model = "claude-opus-4-5-20251101",
                TargetLanguage = "fr",
                AdditionalInstructions = "Translate accurately while maintaining the original meaning"
            });

        // Assert
        TestContext.WriteLine($"Total segments: {result.TotalSegmentsCount}");
        TestContext.WriteLine($"Updated segments: {result.UpdatedSegmentsCount}");
        PrintResult(result);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
        Assert.IsGreaterThan(0, result.TotalSegmentsCount);
        Assert.IsNotNull(result.Usage);
    }

    [TestMethod, ContextDataSource]
    public async Task TranslateText_WithSimpleText_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var translationActions = new TranslationActions(context, FileManager);
        var original = "Brat mir einer einen Storch.";

        // Act
        var result = await translationActions.TranslateText(
            new TranslateTextRequest
            {
                Text = original,
                Model = "claude-haiku-4-5-20251001",
                TargetLanguage = "en-US",
            });

        // Assert
        TestContext.WriteLine($"Original text: {original}");
        TestContext.WriteLine($"Translated text: {result.TranslatedText}");
        TestContext.WriteLine($"System prompt: {result.SystemPrompt}");
        TestContext.WriteLine($"User prompt: {result.UserPrompt}");
        PrintResult(result);

        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.TranslatedText));
        Assert.IsFalse(string.IsNullOrEmpty(result.SystemPrompt));
        Assert.IsFalse(string.IsNullOrEmpty(result.UserPrompt));
        Assert.IsNotNull(result.Usage);
    }
}
