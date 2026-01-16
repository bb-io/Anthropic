using Tests.Anthropic.Base;
using Apps.Anthropic.Actions;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Anthropic;

[TestClass]
public class DeprecatedXliffActionsTests : TestBaseMultipleConnections
{
    [TestMethod, ContextDataSource]
    public async Task GetQualityScores_WithTranslatedXliff_ShouldReturnValidScore(InvocationContext context)
    {
        // Arrange
        var actions = new DeprecatedXliffActions(context, FileManager);
        var xliffRequest = new ProcessXliffRequest
        {
            Xliff = new FileReference
            {
                Name = "Markdown entry #1_en-US-Default_HTML-nl-NL#TR_FQTF#.html.txlf",
                ContentType = "text/xml"
            },
            Model = "claude-3-5-sonnet-20240620",
            SystemPrompt = "You are a professional translator."
        };
        var glossaryRequest = new GlossaryRequest();

        // Act
        var result = await actions.GetQualityScores(xliffRequest, glossaryRequest);

        // Assert
        TestContext.WriteLine($"Average Quality Score: {result.AverageScore}");
        Assert.IsNotNull(result);
        Assert.IsGreaterThanOrEqualTo(result.AverageScore, 0);
        Assert.IsLessThanOrEqualTo(result.AverageScore, 10);
    }

    [TestMethod, ContextDataSource]
    public async Task ProcessXliff_WithXliffFile_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var completionActions = new DeprecatedXliffActions(context, FileManager);

        // Act
        var result = await completionActions.ProcessXliff(
            new ProcessXliffRequest
            {
                Xliff = new FileReference
                {
                    Name = "Markdown entry #1_en-US-Default_HTML-nl-NL#TR_FQTF#.html.txlf"
                },
                Model = "claude-3-5-sonnet-20241022", // Use an appropriate model
                Prompt = "Translate the text accurately while maintaining the original formatting"
            },
            new GlossaryRequest(),
            1500);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Xliff);
        Assert.Contains("Markdown entry", result.Xliff.Name);
    }

    [TestMethod, ContextDataSource]
    public async Task PostEditXliff_WithXliffFile_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var completionActions = new DeprecatedXliffActions(context, FileManager);

        // Act
        var result = await completionActions.PostEditXliff(
            new ProcessXliffRequest
            {
                Xliff = new FileReference
                {
                    Name = "Markdown entry #1_en-US-Default_HTML-nl-NL#TR_FQTF#.html.txlf"
                },
                Model = "claude-3-5-sonnet-20241022", // Use an appropriate model
                Prompt = "Improve the fluency and style of the translations while maintaining meaning"
            },
            new GlossaryRequest(),
            1500);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Xliff);
        Assert.Contains("Markdown entry", result.Xliff.Name);
    }

    [TestMethod, ContextDataSource]
    public async Task GetQualityScores_WithXliffFile_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var completionActions = new DeprecatedXliffActions(context, FileManager);

        // Act
        var result = await completionActions.GetQualityScores(
            new ProcessXliffRequest
            {
                Xliff = new FileReference
                {
                    Name = "Markdown entry #1_en-US-Default_HTML-nl-NL#TR_FQTF#.html.txlf"
                },
                Model = "claude-3-5-sonnet-20241022",
                Prompt = "fluency, grammar, terminology, style, and punctuation"
            },
            new GlossaryRequest());

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.XliffFile);
        Assert.Contains("Markdown entry", result.XliffFile.Name);
        Assert.IsTrue(result.AverageScore >= 0 && result.AverageScore <= 10);
    }
}