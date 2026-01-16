using Tests.Anthropic.Base;
using Apps.Anthropic.Actions;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Anthropic;

[TestClass]
public class ReviewActionsTests : TestBaseMultipleConnections
{
    [TestMethod, ContextDataSource]
    public async Task ReviewContent_WithTranslatedXliff_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var reviewActions = new ReviewActions(context, FileManager);

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
        TestContext.WriteLine($"Total segments processed: {result.TotalSegmentsProcessed}");
        TestContext.WriteLine($"Total segments finalized: {result.TotalSegmentsFinalized}");
        TestContext.WriteLine($"Total segments under threshold: {result.TotalSegmentsUnderThreshhold}");
        TestContext.WriteLine($"Average quality score: {result.AverageMetric:F3}");
        TestContext.WriteLine($"Percentage under threshold: {result.PercentageSegmentsUnderThreshhold:F1}%");
        PrintResult(result);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
        Assert.IsGreaterThan(0, result.TotalSegmentsProcessed);
        Assert.IsNotNull(result.Usage);
        Assert.IsTrue(result.AverageMetric >= 0.0f && result.AverageMetric <= 1.0f);
        Assert.IsTrue(result.PercentageSegmentsUnderThreshhold >= 0.0f && result.PercentageSegmentsUnderThreshhold <= 100.0f);
    }

    [TestMethod, ContextDataSource]
    public async Task ReviewText_WithTranslatedText_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var reviewActions = new ReviewActions(context, FileManager);

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
        PrintResult(result);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Score >= 0.0f && result.Score <= 1.0f);
        Assert.IsFalse(string.IsNullOrEmpty(result.SystemPrompt));
        Assert.IsFalse(string.IsNullOrEmpty(result.UserPrompt));
        Assert.IsNotNull(result.Usage);
    }
}
