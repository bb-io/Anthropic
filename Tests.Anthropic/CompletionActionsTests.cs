using Apps.Anthropic.Actions;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Files;
using FluentAssertions;
using Newtonsoft.Json;
using Tests.Anthropic.Base;

namespace Tests.Anthropic;

[TestClass]
public class CompletionActionsTests : TestBase
{
    [TestMethod]
    public async Task CreateCompletion_WithHelloWorldPrompt_ShouldReturnValidResponse()
    {
        // Arrange
        var actions = new CompletionActions(InvocationContext, FileManager);
        
        // Act
        var response = await actions.CreateCompletion(
            new()
            {
                Prompt = "Hello, world!",
                Model = "claude-3-5-haiku-20241022"
            }, 
            new());

        // Assert
        response.Text.Should().NotBeNullOrEmpty();
        response.Usage.InputTokens.Should().BeGreaterThan(0);

        Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
    }

    [TestMethod]
    public async Task GetQualityScores_WithTranslatedXliff_ShouldReturnValidScore()
    {
        // Arrange
        var actions = new CompletionActions(InvocationContext, FileManager);
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
        result.Should().NotBeNull();
        result.AverageScore.Should().BeGreaterThanOrEqualTo(0);
        result.AverageScore.Should().BeLessThanOrEqualTo(10);

        Console.WriteLine($"Average Quality Score: {result.AverageScore}");
    }

    [TestMethod]
    public async Task ProcessXliff_WithXliffFile_ProcessesSuccessfully()
    {
        // Arrange
        var completionActions = new CompletionActions(InvocationContext, FileManager);

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
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Xliff);
        Assert.IsTrue(result.Xliff.Name.Contains("Markdown entry"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task PostEditXliff_WithXliffFile_ProcessesSuccessfully()
    {
        // Arrange
        var completionActions = new CompletionActions(InvocationContext, FileManager);

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
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Xliff);
        Assert.IsTrue(result.Xliff.Name.Contains("Markdown entry"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task GetQualityScores_WithXliffFile_ProcessesSuccessfully()
    {
        // Arrange
        var completionActions = new CompletionActions(InvocationContext, FileManager);

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
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.XliffFile);
        Assert.IsTrue(result.XliffFile.Name.Contains("Markdown entry"));
        Assert.IsTrue(result.AverageScore >= 0 && result.AverageScore <= 10);

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}