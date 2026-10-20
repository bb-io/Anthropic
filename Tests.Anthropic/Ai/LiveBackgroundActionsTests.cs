using System.Text;
using Apps.Anthropic.Actions;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Polling;
using Apps.OpenAI.Polling.Models;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Tests.Anthropic.Base;

namespace Tests.Anthropic.Ai;

// Live: submits a real Anthropic message batch and waits for it, which usually takes minutes and can
// take up to 24 hours. Skipped unless RUN_LIVE_BATCH=1, so a normal test run never waits on a batch.
[TestClass]
public class LiveBackgroundActionsTests : TestBaseMultipleConnections
{
    private const string RunVariable = "RUN_LIVE_BATCH";
    private const string Model = "claude-sonnet-5";

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(90);

    private readonly InMemoryFileManagementClient _files = new();
    private readonly List<string> _loggedWarnings = [];

    // Runs the way a Bird does: start, the event polls until it fires once, then download.
    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task TranslateInBackground_RealBatch_EventFiresOnceAndDownloadTranslates(InvocationContext context)
    {
        RequireOptIn();
        WithWarningLog(context);
        var file = _files.Add("live-batch.xlf", ThreeUntranslatedSegments);

        var started = await new TranslationActions(context, _files).TranslateInBackground(
            new ModelIdentifier { Model = Model },
            new TranslateContentRequest { File = file, TargetLanguage = "fr", GenerateDocumentSummary = false });

        PrintResult(started);
        Assert.IsFalse(string.IsNullOrWhiteSpace(started.BatchId));
        Assert.AreEqual(3, started.SegmentsToProcessCount);

        await WaitUntilEventFiresOnceAsync(context, started.BatchId);

        var downloaded = await new BackgroundActions(context, _files).DownloadContentFromBackground(
            new BackgroundDownloadRequest { TransformationFile = started.TransformationFile });

        PrintResult(downloaded);
        var segments = Segments(downloaded.File);
        Assert.AreEqual(3, downloaded.UpdatedSegmentsCount);
        Assert.AreEqual(0, downloaded.UnresolvedSegmentsCount);
        Assert.AreEqual("completed", downloaded.BatchStatus);
        Assert.IsGreaterThan(0, downloaded.Usage.InputTokens);
        Assert.IsTrue(segments.All(segment => segment.State == SegmentState.Translated));
        Assert.IsTrue(segments.All(segment => !string.IsNullOrWhiteSpace(segment.GetTarget()) && segment.GetTarget() != segment.GetSource()));
        Assert.IsEmpty(_loggedWarnings, string.Join(Environment.NewLine, _loggedWarnings));
    }

    // A planted mistranslation (cars for AEDs) in the 47-segment contentful file must come back
    // corrected, every segment marked Reviewed and the editor recorded. The document summary runs at
    // start, as it does by default.
    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task EditInBackground_RealBatch_CorrectsPlantedError(InvocationContext context)
    {
        RequireOptIn();
        WithWarningLog(context);
        var xliff = ContentfulFixture.PlantTarget(
            await ContentfulFixture.ReadXliffAsync(FileManager), "Why Philips AEDs?", "Pourquoi les voitures Philips ?");
        var file = _files.Add(ContentfulFixture.Xliff, xliff);

        var started = await new EditActions(context, _files).EditInBackground(
            new ModelIdentifier { Model = Model },
            new EditContentRequest { File = file, ModifiedBy = "Blackbird background tests" });

        PrintResult(started);
        Assert.IsFalse(string.IsNullOrWhiteSpace(started.BatchId));
        Assert.AreEqual(ContentfulFixture.XliffSegments, started.SegmentsToProcessCount);

        await WaitUntilEventFiresOnceAsync(context, started.BatchId);

        var downloaded = await new BackgroundActions(context, _files).DownloadContentFromBackground(
            new BackgroundDownloadRequest { TransformationFile = started.TransformationFile });

        PrintResult(downloaded);
        var segments = Segments(downloaded.File);
        var planted = segments.Where(segment => segment.GetSource() == "Why Philips AEDs?").Select(segment => segment.GetTarget()).ToList();
        TestContext.WriteLine($"Edited planted segments: {string.Join(" | ", planted)}");
        Assert.AreEqual(0, downloaded.UnresolvedSegmentsCount);
        Assert.AreEqual(ContentfulFixture.XliffSegments, downloaded.ProcessedSegmentsCount);
        Assert.IsGreaterThanOrEqualTo(1, downloaded.UpdatedSegmentsCount);
        Assert.IsTrue(planted.All(target => !target.Contains("voitures") && target.Contains("DEA")));
        Assert.IsTrue(segments.All(segment => segment.State == SegmentState.Reviewed));
        Assert.Contains("modified-by=\"Blackbird background tests\"", _files.Read(downloaded.File));
        Assert.IsNull(downloaded.AverageScore);
        Assert.IsEmpty(_loggedWarnings, string.Join(Environment.NewLine, _loggedWarnings));
    }

    // A planted nonsense target must score below 0.6 and below the average; every segment gets a score
    // and the download reports the average on the 0..1 scale.
    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task ReviewInBackground_RealBatch_ScoresPlantedNonsenseLowest(InvocationContext context)
    {
        RequireOptIn();
        WithWarningLog(context);
        var xliff = ContentfulFixture.PlantTarget(
            await ContentfulFixture.ReadXliffAsync(FileManager), "Demonstrated results headline", "Der Mond ist aus grünem Käse.");
        var file = _files.Add(ContentfulFixture.Xliff, xliff);

        var started = await new ReviewActions(context, _files).ReviewInBackground(
            new ModelIdentifier { Model = Model },
            new ReviewContentRequest { File = file });

        PrintResult(started);
        Assert.AreEqual(ContentfulFixture.XliffSegments, started.SegmentsToProcessCount);

        await WaitUntilEventFiresOnceAsync(context, started.BatchId);

        var downloaded = await new BackgroundActions(context, _files).DownloadContentFromBackground(
            new BackgroundDownloadRequest { TransformationFile = started.TransformationFile });

        PrintResult(downloaded);
        var scores = Segments(downloaded.File)
            .Select(segment => (Source: segment.GetSource(), Score: ContentfulFixture.Score(segment)))
            .ToList();
        var planted = scores.First(score => score.Source == "Demonstrated results headline").Score!.Value;
        TestContext.WriteLine($"Planted segment score: {planted}, average: {downloaded.AverageScore}");
        Assert.AreEqual(0, downloaded.UnresolvedSegmentsCount);
        Assert.IsTrue(scores.All(score => score.Score is >= 0 and <= 1), "Every segment must carry a score.");
        Assert.IsNotNull(downloaded.AverageScore);
        Assert.IsLessThan(0.6, planted);
        Assert.IsLessThan(downloaded.AverageScore.Value, planted);
        Assert.IsEmpty(_loggedWarnings, string.Join(Environment.NewLine, _loggedWarnings));
    }

    private static void RequireOptIn()
    {
        if (Environment.GetEnvironmentVariable(RunVariable) != "1")
        {
            Assert.Inconclusive($"Set {RunVariable}=1 to submit a real batch.");
        }
    }

    // Warnings are not part of the action output; the library logs them through InvocationContext.Logger.
    private void WithWarningLog(InvocationContext context) =>
        context.Logger = new Logger("Debug") { LogWarning = (message, _) => _loggedWarnings.Add(message ?? string.Empty) };

    // Polls the event the way the platform does until it fires, then once more to prove it fires only once.
    private async Task WaitUntilEventFiresOnceAsync(InvocationContext context, string batchId)
    {
        var polling = new BatchPollingList(context);
        var identifier = new BatchIdentifier { BatchId = batchId };
        var memory = (await polling.OnBatchFinished(new PollingEventRequest<BatchMemory>(), identifier)).Memory!;
        var waited = TimeSpan.Zero;
        PollingEventResponse<BatchMemory, BatchResponse> poll;

        do
        {
            await Task.Delay(PollInterval);
            waited += PollInterval;
            poll = await polling.OnBatchFinished(new PollingEventRequest<BatchMemory> { Memory = memory }, identifier);
            memory = poll.Memory!;
            TestContext.WriteLine($"{waited}: {poll.Result?.ProcessingStatus}, fired: {poll.FlyBird}");
        }
        while (!poll.FlyBird && waited < MaxWait);

        Assert.IsTrue(poll.FlyBird, $"The batch did not finish within {MaxWait}.");

        var again = await polling.OnBatchFinished(new PollingEventRequest<BatchMemory> { Memory = memory }, identifier);
        Assert.IsFalse(again.FlyBird, "The event must fire only once.");
    }

    private const string ThreeUntranslatedSegments = """
        <?xml version="1.0" encoding="utf-8"?>
        <xliff version="2.0" srcLang="en" trgLang="fr" xmlns="urn:oasis:names:tc:xliff:document:2.0">
          <file id="f1" original="live-batch.xlf">
            <unit id="u1"><segment id="s1"><source>The weather is lovely today.</source></segment></unit>
            <unit id="u2"><segment id="s2"><source>Please restart the application.</source></segment></unit>
            <unit id="u3"><segment id="s3"><source>Our office opens at nine.</source></segment></unit>
          </file>
        </xliff>
        """;

    private List<Segment> Segments(FileReference file)
    {
        var loaded = Transformation.Load(new MemoryStream(Encoding.UTF8.GetBytes(_files.Read(file))), file.Name);
        Assert.IsTrue(loaded.Success, loaded.Error);
        return loaded.Value.GetUnits().SelectMany(unit => unit.Segments).ToList();
    }
}
