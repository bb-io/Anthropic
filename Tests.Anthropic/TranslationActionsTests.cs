using Apps.Anthropic.Actions;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;
using Tests.Anthropic.Base;

namespace Tests.Anthropic;

[TestClass]
public class TranslationActionsTests : TestBase
{
    [TestMethod]
    public async Task Translate_WithSimpleXliff_ProcessesSuccessfully()
    {
        // Arrange
        var translationActions = new TranslationActions(InvocationContext, FileManager);

        // Act
        var result = await translationActions.Translate(
            new TranslateContentRequest
            {
                File = new FileReference
                {
                    Name = "contentful.html",
                    ContentType = "application/xliff+xml"
                },
                Model = "claude-3-5-sonnet-20241022",
                TargetLanguage = "fr",
                AdditionalInstructions = "Translate accurately while maintaining the original meaning"
            });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
        Assert.IsTrue(result.TotalSegmentsCount > 0);
        Assert.IsNotNull(result.Usage);

        Console.WriteLine($"Total segments: {result.TotalSegmentsCount}");
        Console.WriteLine($"Updated segments: {result.UpdatedSegmentsCount}");
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task TranslateText_WithSimpleText_ProcessesSuccessfully()
    {
        // Arrange
        var translationActions = new TranslationActions(InvocationContext, FileManager);

        // Act
        var result = await translationActions.TranslateText(
            new TranslateTextRequest
            {
                Text = "Hello, how are you today? I hope you're having a wonderful day!",
                Model = "claude-3-5-sonnet-20241022",
                TargetLanguage = "es",
                AdditionalInstructions = "Use formal Spanish"
            });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.TranslatedText));
        Assert.IsFalse(string.IsNullOrEmpty(result.SystemPrompt));
        Assert.IsFalse(string.IsNullOrEmpty(result.UserPrompt));
        Assert.IsNotNull(result.Usage);

        Console.WriteLine($"Original text: Hello, how are you today? I hope you're having a wonderful day!");
        Console.WriteLine($"Translated text: {result.TranslatedText}");
        Console.WriteLine($"System prompt: {result.SystemPrompt}");
        Console.WriteLine($"User prompt: {result.UserPrompt}");
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}
