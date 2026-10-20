using System.Text;
using Apps.Anthropic.Actions;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Request.Optional;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Tests.Anthropic.Base;

namespace Tests.Anthropic.Ai;

// End to end against the real API through the same ILlmClient that runs in production.
[TestClass]
public class LiveContentActionsTests : TestBaseMultipleConnections
{
    private const string Model = "claude-sonnet-5";
    private const string ContentfulHtml = ContentfulFixture.Html;
    private const string ContentfulXliff = ContentfulFixture.Xliff;
    private const int ContentfulXliffSegments = ContentfulFixture.XliffSegments;

    private readonly InMemoryFileManagementClient _files = new();
    private readonly List<string> _loggedWarnings = [];

    // A 47-segment page, so the document summary runs too. Every segment comes back translated with
    // nothing lost, and usage comes from the provider's own counts.
    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task Translate_ContentfulHtml_TranslatesEverySegment(InvocationContext context)
    {
        var file = _files.Add(ContentfulHtml, await ContentfulFixture.ReadAsync(FileManager, ContentfulHtml), "text/html");

        var result = await new TranslationActions(WithWarningLog(context), _files).Translate(
            new ModelIdentifier { Model = Model },
            new TranslateContentRequest { File = file, TargetLanguage = "fr" },
            new OptionalSkillRequest());

        PrintResult(result);
        var segments = Segments(result.File).Where(segment => !segment.IsIgnorbale).ToList();
        AssertNoWarningsLogged();
        Assert.AreEqual(segments.Count, (int)result.UpdatedSegmentsCount);
        Assert.IsTrue(segments.All(segment => segment.State == SegmentState.Translated));
        Assert.IsTrue(segments.All(segment => !string.IsNullOrWhiteSpace(segment.GetTarget())));
        Assert.IsGreaterThan(0, result.Usage.InputTokens);
        Assert.IsNotEmpty(result.Prompts);
    }

    // The HTML page has no source language set by the action: it is detected, and the page comes back
    // as HTML when the original format is requested.
    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task Translate_ContentfulHtmlToOriginal_ReturnsTranslatedHtml(InvocationContext context)
    {
        var file = _files.Add(ContentfulHtml, await ContentfulFixture.ReadAsync(FileManager, ContentfulHtml), "text/html");

        var result = await new TranslationActions(WithWarningLog(context), _files).Translate(
            new ModelIdentifier { Model = Model },
            new TranslateContentRequest { File = file, TargetLanguage = "fr", OutputFileHandling = "original", GenerateDocumentSummary = false },
            new OptionalSkillRequest());

        PrintResult(result);
        var html = _files.Read(result.File);
        TestContext.WriteLine(html);
        AssertNoWarningsLogged();
        Assert.Contains("data-contentful-entry-id", html);
        Assert.DoesNotContain("Why Philips AEDs?", html);
        Assert.IsGreaterThan(0, result.UpdatedSegmentsCount);
    }

    // A planted mistranslation (cars for AEDs) must be corrected, every translated segment marked
    // Reviewed, and the editor recorded on the changed unit.
    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task Edit_ContentfulXliffWithPlantedError_CorrectsIt(InvocationContext context)
    {
        var xliff = ContentfulFixture.PlantTarget(await ContentfulFixture.ReadXliffAsync(FileManager), "Why Philips AEDs?", "Pourquoi les voitures Philips ?");
        var file = _files.Add(ContentfulXliff, xliff);

        var result = await new EditActions(WithWarningLog(context), _files).EditContent(
            new ModelIdentifier { Model = Model },
            new EditContentRequest { File = file, ModifiedBy = "Blackbird tests" },
            new OptionalSkillRequest());

        PrintResult(result);
        var segments = Segments(result.File);
        var planted = segments.Where(segment => segment.GetSource() == "Why Philips AEDs?").Select(segment => segment.GetTarget()).ToList();
        TestContext.WriteLine($"Edited planted segments: {string.Join(" | ", planted)}");
        AssertNoWarningsLogged();
        Assert.AreEqual(ContentfulXliffSegments, result.TotalSegmentsReviewed);
        Assert.IsGreaterThanOrEqualTo(1, result.TotalSegmentsUpdated);
        Assert.IsTrue(planted.All(target => !target.Contains("voitures") && target.Contains("DEA")));
        Assert.IsTrue(segments.All(segment => segment.State == SegmentState.Reviewed));
        Assert.Contains("modified-by=\"Blackbird tests\"", _files.Read(result.File));
    }

    // A planted nonsense target must score below the good ones, every segment gets a score, and the
    // summary keeps its existing semantics (under 0.6, percentage on 0..100).
    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task Review_ContentfulXliffWithPlantedNonsense_ScoresItLowest(InvocationContext context)
    {
        var xliff = ContentfulFixture.PlantTarget(await ContentfulFixture.ReadXliffAsync(FileManager), "Demonstrated results headline", "Der Mond ist aus grünem Käse.");
        var file = _files.Add(ContentfulXliff, xliff);

        var result = await new ReviewActions(WithWarningLog(context), _files).ReviewContent(
            new ModelIdentifier { Model = Model },
            new ReviewContentRequest { File = file },
            new OptionalSkillRequest());

        PrintResult(result);
        var scores = Segments(result.File)
            .Select(segment => (Source: segment.GetSource(), Score: ContentfulFixture.Score(segment)))
            .ToList();
        AssertNoWarningsLogged();
        Assert.AreEqual(ContentfulXliffSegments, result.TotalSegmentsProcessed);
        Assert.IsTrue(scores.All(score => score.Score is >= 0 and <= 1), "Every segment must carry a score.");
        var planted = scores.First(score => score.Source == "Demonstrated results headline").Score!.Value;
        TestContext.WriteLine($"Planted segment score: {planted}");
        Assert.IsLessThan(0.6, planted);
        Assert.IsLessThan(result.AverageMetric, planted);
        Assert.IsGreaterThanOrEqualTo(1, result.TotalSegmentsUnderThreshhold);
        Assert.IsTrue(result.PercentageSegmentsUnderThreshhold is > 0 and <= 100);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task TranslateText_RealModel_ReturnsTranslation(InvocationContext context)
    {
        const string source = "Please restart the application.";

        var result = await new TranslationActions(WithWarningLog(context), _files).TranslateText(
            new ModelIdentifier { Model = Model },
            new TranslateTextRequest { Text = source, SourceLanguage = "en", TargetLanguage = "fr" },
            new OptionalSkillRequest());

        PrintResult(result);
        AssertNoWarningsLogged();
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.TranslatedText));
        Assert.AreNotEqual(source, result.TranslatedText);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.SystemPrompt));
        Assert.Contains(source, result.UserPrompt);
        Assert.IsGreaterThan(0, result.Usage.InputTokens);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task EditText_RealModel_CorrectsMistranslationAndKeepsCorrectTarget(InvocationContext context)
    {
        var actions = new EditActions(WithWarningLog(context), _files);

        var wrong = await actions.EditText(
            new ModelIdentifier { Model = Model },
            new EditTextRequest { SourceText = "The cat is sleeping on the sofa.", TargetText = "Le chien dort sur le canapé.", SourceLanguage = "en", TargetLanguage = "fr" },
            new OptionalSkillRequest());
        var correct = await actions.EditText(
            new ModelIdentifier { Model = Model },
            new EditTextRequest { SourceText = "Good morning.", TargetText = "Bonjour.", SourceLanguage = "en", TargetLanguage = "fr" },
            new OptionalSkillRequest());

        PrintResult(wrong);
        PrintResult(correct);
        AssertNoWarningsLogged();
        Assert.Contains("chat", wrong.EditedText);
        Assert.AreEqual("Bonjour.", correct.EditedText);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task ReviewText_RealModel_ScoresGoodAboveBad(InvocationContext context)
    {
        var actions = new ReviewActions(WithWarningLog(context), _files);

        var good = await actions.ReviewText(
            new ModelIdentifier { Model = Model },
            new ReviewTextRequest { SourceText = "Please restart the application.", TargetText = "Veuillez redémarrer l'application.", SourceLanguage = "en", TargetLanguage = "fr" },
            new OptionalSkillRequest());
        var bad = await actions.ReviewText(
            new ModelIdentifier { Model = Model },
            new ReviewTextRequest { SourceText = "Our office opens at nine.", TargetText = "La lune est faite de fromage vert.", SourceLanguage = "en", TargetLanguage = "fr" },
            new OptionalSkillRequest());

        PrintResult(good);
        PrintResult(bad);
        AssertNoWarningsLogged();
        Assert.IsTrue(good.Score is >= 0 and <= 1 && bad.Score is >= 0 and <= 1);
        Assert.IsGreaterThan(bad.Score, good.Score, $"good {good.Score} vs bad {bad.Score}");
    }

    // Warnings are not part of the action output; the library logs them through InvocationContext.Logger.
    private InvocationContext WithWarningLog(InvocationContext context)
    {
        context.Logger = new Logger("Debug") { LogWarning = (message, _) => _loggedWarnings.Add(message ?? string.Empty) };
        return context;
    }

    private void AssertNoWarningsLogged() => Assert.IsEmpty(_loggedWarnings, string.Join(Environment.NewLine, _loggedWarnings));

    private List<Segment> Segments(FileReference file)
    {
        var loaded = Transformation.Load(new MemoryStream(Encoding.UTF8.GetBytes(_files.Read(file))), file.Name);
        Assert.IsTrue(loaded.Success, loaded.Error);
        return loaded.Value.GetUnits().SelectMany(unit => unit.Segments).ToList();
    }
}
