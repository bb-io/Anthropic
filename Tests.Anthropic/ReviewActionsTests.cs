using Apps.Anthropic.Actions;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;
using Tests.Anthropic.Base;

namespace Tests.Anthropic;

[TestClass]
public class ReviewActionsTests : TestBase
{
    [TestMethod]
    public async Task ReviewContent_WithTranslatedXliff_ProcessesSuccessfully()
    {
        // Arrange
        var reviewActions = new ReviewActions(InvocationContext, FileManager);

        // Act
        var result = await reviewActions.ReviewContent(
            new ReviewContentRequest
            {
                File = new FileReference
                {
                    Name = "contentful_review.xlf",
                    ContentType = "application/xliff+xml"
                },
                Model = "claude-3-5-sonnet-20241022",
                SourceLanguage = "en",
                TargetLanguage = "fr",
                AdditionalInstructions = "Focus on technical terminology accuracy and cultural adaptation"
            });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
        Assert.IsTrue(result.TotalSegmentsProcessed > 0);
        Assert.IsNotNull(result.Usage);
        Assert.IsTrue(result.AverageMetric >= 0.0f && result.AverageMetric <= 1.0f);
        Assert.IsTrue(result.PercentageSegmentsUnderThreshhold >= 0.0f && result.PercentageSegmentsUnderThreshhold <= 100.0f);

        Console.WriteLine($"Total segments processed: {result.TotalSegmentsProcessed}");
        Console.WriteLine($"Total segments finalized: {result.TotalSegmentsFinalized}");
        Console.WriteLine($"Total segments under threshold: {result.TotalSegmentsUnderThreshhold}");
        Console.WriteLine($"Average quality score: {result.AverageMetric:F3}");
        Console.WriteLine($"Percentage under threshold: {result.PercentageSegmentsUnderThreshhold:F1}%");
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task ReviewText_WithTranslatedText_ProcessesSuccessfully()
    {
        // Arrange
        var reviewActions = new ReviewActions(InvocationContext, FileManager);

        // Act
        var result = await reviewActions.ReviewText(
            new ReviewTextRequest
            {
                SourceText = "Hello, how are you today? I hope you're having a wonderful day!",
                TargetText = "Hola, ¿cómo estás hoy? ¡Espero que tengas un día maravilloso!",
                Model = "claude-3-5-sonnet-20241022",
                SourceLanguage = "en",
                TargetLanguage = "es",
                AdditionalInstructions = "Focus on naturalness and accuracy of the translation"
            });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Score >= 0.0f && result.Score <= 1.0f);
        Assert.IsFalse(string.IsNullOrEmpty(result.SystemPrompt));
        Assert.IsFalse(string.IsNullOrEmpty(result.UserPrompt));
        Assert.IsNotNull(result.Usage);

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}
