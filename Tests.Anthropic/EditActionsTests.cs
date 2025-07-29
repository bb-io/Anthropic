using Apps.Anthropic.Actions;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;
using Tests.Anthropic.Base;

namespace Tests.Anthropic;

[TestClass]
public class EditActionsTests : TestBase
{
    [TestMethod]
    public async Task EditContent_WithTranslatedXliff_ProcessesSuccessfully()
    {
        // Arrange
        var editActions = new EditActions(InvocationContext, FileManager);

        // Act
        var result = await editActions.EditContent(
            new EditContentRequest
            {
                File = new FileReference
                {
                    Name = "contentful.html.xlf",
                    ContentType = "application/xliff+xml"
                },
                Model = "claude-3-5-sonnet-20241022",
                SourceLanguage = "en",
                TargetLanguage = "fr",
                AdditionalInstructions = "Improve fluency and ensure natural expression while maintaining accuracy"
            });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
        Assert.IsTrue(result.TotalSegmentsReviewed >= 0);
        Assert.IsNotNull(result.Usage);

        Console.WriteLine($"Total segments reviewed: {result.TotalSegmentsReviewed}");
        Console.WriteLine($"Total segments updated: {result.TotalSegmentsUpdated}");
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
    
    [TestMethod]
    public async Task EditText_WithTranslatedText_ProcessesSuccessfully()
    {
        // Arrange
        var editActions = new EditActions(InvocationContext, FileManager);

        // Act
        var result = await editActions.EditText(
            new EditTextRequest
            {
                SourceText = "Hello, how are you today? I hope you're having a wonderful day!",
                TargetText = "Hola, ¿cómo estás hoy? ¡Espero que tengas un día maravilloso!",
                SourceLanguage = "en",
                TargetLanguage = "es",
                TargetAudience = "Professional business context",
                Model = "claude-3-5-sonnet-20241022",
                AdditionalInstructions = "Ensure the tone is formal and professional"
            });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.EditedText));
        Assert.IsFalse(string.IsNullOrEmpty(result.SystemPrompt));
        Assert.IsFalse(string.IsNullOrEmpty(result.UserPrompt));
        Assert.IsNotNull(result.Usage);
        
        Console.WriteLine($"Edited text: {result.EditedText}");
        Console.WriteLine($"System prompt: {result.SystemPrompt}");
        Console.WriteLine($"User prompt: {result.UserPrompt}");
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}
