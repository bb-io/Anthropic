using System.Runtime.CompilerServices;
using System.Text;
using Apps.Anthropic.Actions;
using Apps.Anthropic.Ai;
using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Entities;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Polling;
using Apps.OpenAI.Polling.Models;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tests.Anthropic.Ai;

// Offline, no API spend: start → download round trips against a scripted batch provider. The
// synchronous client fails the test if it is called; the summary is off and the files carry their
// languages, so starting a job never needs it.
[TestClass]
public class BackgroundActionsOfflineTests
{
    private const string Model = "claude-sonnet-5";

    private readonly InMemoryFileManagementClient _files = new();
    private readonly ScriptedBatchClient _batch = new();
    private readonly List<string> _loggedWarnings = [];

    [TestMethod]
    public async Task TranslateInBackground_ThenDownload_TranslatesEverySegment()
    {
        var file = _files.Add("translate.xlf", Xliff(Seg("One"), Seg("Two")));

        var started = await Translation().TranslateInBackground(
            new ModelIdentifier { Model = Model },
            new TranslateContentRequest { File = file, TargetLanguage = "de", GenerateDocumentSummary = false });

        Assert.AreEqual(ScriptedBatchClient.JobId, started.BatchId);
        Assert.AreEqual("running", started.Status);
        Assert.AreEqual(1, started.RequestsCount);
        Assert.AreEqual(2, started.SegmentsToProcessCount);
        Assert.IsFalse(string.IsNullOrWhiteSpace(started.SystemPrompt));

        _batch.State = BatchJobState.Completed;
        var downloaded = await Background().DownloadContentFromBackground(
            new BackgroundDownloadRequest { TransformationFile = started.TransformationFile });

        var segments = Segments(downloaded.File);
        Assert.AreEqual(2, downloaded.UpdatedSegmentsCount);
        Assert.AreEqual(2, downloaded.ProcessedSegmentsCount);
        Assert.AreEqual(0, downloaded.UnresolvedSegmentsCount);
        Assert.AreEqual("completed", downloaded.BatchStatus);
        Assert.IsNull(downloaded.AverageScore);
        Assert.AreEqual(20, downloaded.Usage.InputTokens);
        Assert.AreEqual(10, downloaded.Usage.OutputTokens);
        CollectionAssert.AreEqual(new[] { "DE:One", "DE:Two" }, segments.Select(segment => segment.GetTarget()).ToArray());
        Assert.IsTrue(segments.All(segment => segment.State == SegmentState.Translated));
        Assert.IsEmpty(_loggedWarnings, string.Join(Environment.NewLine, _loggedWarnings));
    }

    [TestMethod]
    public async Task EditInBackground_ThenDownload_MarksSegmentsReviewed()
    {
        var file = _files.Add("edit.xlf", Xliff(Seg("One", "Eins", "translated"), Seg("Two", "Zwo", "translated")));

        var started = await Edit().EditInBackground(
            new ModelIdentifier { Model = Model },
            new EditContentRequest { File = file, GenerateDocumentSummary = false, ModifiedBy = "Background test" });

        _batch.State = BatchJobState.Completed;
        var downloaded = await Background().DownloadContentFromBackground(
            new BackgroundDownloadRequest { TransformationFile = started.TransformationFile, BatchId = started.BatchId });

        var segments = Segments(downloaded.File);
        Assert.AreEqual(2, downloaded.UpdatedSegmentsCount);
        Assert.IsTrue(segments.All(segment => segment.State == SegmentState.Reviewed));
        Assert.IsTrue(segments.All(segment => segment.GetTarget().StartsWith("EDITED:")));
        Assert.Contains("modified-by=\"Background test\"", _files.Read(downloaded.File));
    }

    // Review in background scores through the same writer as Review, with the library's eligibility:
    // a locked unit and an Initial segment are not sent.
    [TestMethod]
    public async Task ReviewInBackground_ThenDownload_ScoresEligibleSegmentsAndReportsAverage()
    {
        var file = _files.Add("review.xlf", Xliff(
            Seg("One", "Eins", "translated"),
            Seg("Two", "Zwei", "reviewed"),
            Seg("Three", "Drei", "initial"),
            Seg("Four", "Vier", "translated", locked: true)));

        var started = await Review().ReviewInBackground(
            new ModelIdentifier { Model = Model },
            new ReviewContentRequest { File = file, GenerateDocumentSummary = false });

        Assert.AreEqual(2, started.SegmentsToProcessCount);

        _batch.State = BatchJobState.Completed;
        var downloaded = await Background().DownloadContentFromBackground(
            new BackgroundDownloadRequest { TransformationFile = started.TransformationFile });

        var scored = Segments(downloaded.File)
            .Where(segment => segment.TargetAttributes.Any(attribute => attribute.Name.LocalName == "extradata"))
            .Select(segment => segment.GetSource())
            .ToList();
        Assert.AreEqual(0.9f, downloaded.AverageScore!.Value, 0.0001f);
        CollectionAssert.AreEquivalent(new[] { "One", "Two" }, scored);
    }

    [TestMethod]
    public async Task TranslateInBackground_NothingToProcess_CreatesNoBatchAndDownloadReturnsFileUnchanged()
    {
        var file = _files.Add("done.xlf", Xliff(Seg("One", "Eins", "translated")));

        var started = await Translation().TranslateInBackground(
            new ModelIdentifier { Model = Model },
            new TranslateContentRequest { File = file, TargetLanguage = "de", GenerateDocumentSummary = false });

        Assert.AreEqual(string.Empty, started.BatchId);
        Assert.AreEqual("completed", started.Status);
        Assert.IsEmpty(_batch.Requests);

        var downloaded = await Background().DownloadContentFromBackground(
            new BackgroundDownloadRequest { TransformationFile = started.TransformationFile });

        Assert.AreEqual(0, downloaded.UpdatedSegmentsCount);
        Assert.AreEqual("completed", downloaded.BatchStatus);
        Assert.AreEqual("Eins", Segments(downloaded.File).Single().GetTarget());
    }

    [TestMethod]
    public async Task DownloadContentFromBackground_JobStillRunning_ThrowsMisconfiguration()
    {
        var file = _files.Add("running.xlf", Xliff(Seg("One")));
        var started = await Translation().TranslateInBackground(
            new ModelIdentifier { Model = Model },
            new TranslateContentRequest { File = file, TargetLanguage = "de", GenerateDocumentSummary = false });

        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            Background().DownloadContentFromBackground(new BackgroundDownloadRequest { TransformationFile = started.TransformationFile }));

        Assert.Contains("has not finished", exception.Message);
    }

    // A file that was never started in the background carries no job; the message says which file to pass.
    [TestMethod]
    public async Task DownloadContentFromBackground_FileWithoutJob_ThrowsMisconfiguration()
    {
        var file = _files.Add("plain.xlf", Xliff(Seg("One", "Eins", "translated")));

        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            Background().DownloadContentFromBackground(new BackgroundDownloadRequest { TransformationFile = file }));

        Assert.Contains("holds no background job", exception.Message);
    }

    // With nothing to process there is no batch; the event fires on its first check, and only once.
    [TestMethod]
    public async Task OnBatchFinished_EmptyBatchId_FiresOnceAndStaysTriggered()
    {
        var polling = new BatchPollingList(Context());
        var identifier = new BatchIdentifier { BatchId = string.Empty };

        var first = await polling.OnBatchFinished(Poll(new BatchMemory()), identifier);
        var second = await polling.OnBatchFinished(Poll(first.Memory!), identifier);

        Assert.IsTrue(first.FlyBird);
        Assert.IsFalse(second.FlyBird);
        Assert.IsTrue(second.Memory!.Triggered);
    }

    [TestMethod]
    [DataRow("in_progress", null, 0, 0, 0, BatchJobState.Running)]
    [DataRow("canceling", null, 0, 0, 0, BatchJobState.Running)]
    [DataRow("ended", null, 2, 0, 0, BatchJobState.Completed)]
    [DataRow("ended", null, 1, 1, 0, BatchJobState.Completed)]
    [DataRow("ended", null, 0, 2, 0, BatchJobState.Failed)]
    [DataRow("ended", null, 1, 0, 1, BatchJobState.Expired)]
    [DataRow("ended", "2026-10-20T10:00:00Z", 1, 0, 0, BatchJobState.Cancelled)]
    public void StateOf_MapsAnthropicBatchStates(string status, string? cancelInitiatedAt, int succeeded, int errored, int expired, BatchJobState expected)
    {
        var batch = new BatchResponse
        {
            ProcessingStatus = status,
            CancelInitiatedAt = cancelInitiatedAt is null ? null : DateTime.Parse(cancelInitiatedAt),
            RequestCounts = new RequestCountResponse { Succeeded = succeeded, Errored = errored, Expired = expired }
        };

        Assert.AreEqual(expected, AnthropicBatchLlmClient.StateOf(batch));
    }

    // A batch request must carry the body a direct call sends, and a result is read like a direct response.
    [TestMethod]
    public async Task AnthropicBatchLlmClient_SubmitAndResults_MatchTheDirectCall()
    {
        var provider = new FakeBatchingApi();
        var client = new AnthropicBatchLlmClient(provider, "claude-haiku-4-5-20251001", new AnthropicLlmSettings(Temperature: "0.2", MaxTokens: 1_000));

        await client.SubmitAsync([new BatchLlmRequest("b0", new LlmRequest("system text", "user text"))], CancellationToken.None);

        var line = JObject.FromObject(provider.Submitted.Single());
        var body = (JObject)line["params"]!;
        Assert.AreEqual("b0", line["custom_id"]!.Value<string>());
        Assert.AreEqual("claude-haiku-4-5-20251001", body["model"]!.Value<string>());
        Assert.AreEqual("system text", body["system"]!.Value<string>());
        Assert.AreEqual(1_000, body["max_tokens"]!.Value<int>());
        Assert.AreEqual(0.2, body["temperature"]!.Value<double>(), 0.0001);
        Assert.AreEqual("user text", body["messages"]![0]!["content"]!.Value<string>());
        Assert.IsNull(body["container"], "Skills are not sent in batches.");

        var truncated = client.ToResult(Line("b0", "succeeded", "{\"translations\":[", "max_tokens"));
        var errored = client.ToResult(JsonConvert.DeserializeObject<BatchRequestDto>(
            """{"custom_id":"b1","result":{"type":"errored","error":{"type":"error","error":{"type":"invalid_request_error","message":"max_tokens is too large"}}}}""")!);
        var expired = client.ToResult(JsonConvert.DeserializeObject<BatchRequestDto>("""{"custom_id":"b2","result":{"type":"expired"}}""")!);

        Assert.AreEqual(FinishReason.Truncated, truncated.Response!.Finish);
        Assert.AreEqual(new LlmUsage(20, 10), truncated.Response.Usage);
        Assert.AreEqual(3, client.Usage.CacheReadInputTokens, "Cache counters are kept from the result lines.");
        Assert.AreEqual("max_tokens is too large", errored.Error);
        Assert.IsNull(errored.Response);
        Assert.Contains("expired", expired.Error!);
    }

    private TranslationActions Translation() => new ScriptedTranslationActions(Context(), _files, _batch);

    private EditActions Edit() => new ScriptedEditActions(Context(), _files, _batch);

    private ReviewActions Review() => new ScriptedReviewActions(Context(), _files, _batch);

    private BackgroundActions Background() => new ScriptedBackgroundActions(Context(), _files, _batch);

    private InvocationContext Context() => new()
    {
        AuthenticationCredentialsProviders =
        [
            new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.AnthropicNative),
            new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused")
        ],
        Logger = new Logger("Debug") { LogWarning = (message, _) => _loggedWarnings.Add(message ?? string.Empty) }
    };

    private static PollingEventRequest<BatchMemory> Poll(BatchMemory memory) => new() { Memory = memory };

    private List<Segment> Segments(FileReference file)
    {
        var loaded = Transformation.Load(new MemoryStream(Encoding.UTF8.GetBytes(_files.Read(file))), file.Name);
        Assert.IsTrue(loaded.Success, loaded.Error);
        return loaded.Value.GetUnits().SelectMany(unit => unit.Segments).ToList();
    }

    private static BatchRequestDto Line(string customId, string type, string text, string stopReason) => new()
    {
        CustomId = customId,
        Result = new ResultDto
        {
            Type = type,
            Message = new MessageDto
            {
                Content = [new ContentItemDto { Type = "text", Text = text }],
                StopReason = stopReason,
                Usage = new UsageResponse { InputTokens = 20, OutputTokens = 10, CacheReadInputTokens = 3 }
            }
        }
    };

    private static (string Source, string? Target, string? State, bool Locked) Seg(
        string source, string? target = null, string? state = null, bool locked = false) => (source, target, state, locked);

    private static string Xliff(params (string Source, string? Target, string? State, bool Locked)[] segments)
    {
        var units = segments.Select((segment, index) =>
            $"""
             <unit id="u{index + 1}"{(segment.Locked ? " translate=\"no\"" : "")}>
               <segment id="s{index + 1}"{(segment.State is null ? "" : $" state=\"{segment.State}\"")}>
                 <source>{segment.Source}</source>{(segment.Target is null ? "" : $"<target>{segment.Target}</target>")}
               </segment>
             </unit>
             """);

        return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <xliff version="2.0" srcLang="en" trgLang="de" xmlns="urn:oasis:names:tc:xliff:document:2.0">
                  <file id="f1" original="test.xlf">
                {string.Join(Environment.NewLine, units)}
                  </file>
                </xliff>
                """;
    }

    // Answers every request by reading the items out of its user prompt. Translate and Edit return a
    // marked text, Review a fixed score.
    private sealed class ScriptedBatchClient : IBatchLlmClient
    {
        public const string JobId = "msgbatch_offline";

        public List<BatchLlmRequest> Requests { get; } = [];

        public BatchJobState State { get; set; } = BatchJobState.Running;

        public LlmCapabilities Capabilities { get; } = new(SupportsStructuredOutput: false, ContextTokens: 200_000, MaxOutputTokens: 8_192);

        public Task<BatchJob> SubmitAsync(IReadOnlyList<BatchLlmRequest> requests, CancellationToken cancellationToken)
        {
            Requests.AddRange(requests);
            return Task.FromResult(new BatchJob(JobId, BatchJobState.Running, requests.Count, CreatedAt: DateTimeOffset.UtcNow));
        }

        public Task<BatchJob> GetJobAsync(string jobId, CancellationToken cancellationToken) =>
            Task.FromResult(new BatchJob(jobId, State, Requests.Count, Requests.Count));

        public async IAsyncEnumerable<BatchLlmResult> GetResultsAsync(BatchJob job, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            foreach (var request in Requests)
            {
                // Read through the real result mapping, as the Anthropic batch client does.
                var anthropic = new AnthropicBatchLlmClient(new FakeBatchingApi(), null, new AnthropicLlmSettings());
                yield return anthropic.ToResult(Line(request.CustomId, "succeeded", Answer(request.Request), "end_turn"));
            }
        }

        private static string Answer(LlmRequest request)
        {
            var prompt = request.UserPrompt;
            var items = JArray.Parse(prompt[prompt.IndexOf('[')..(prompt.LastIndexOf(']') + 1)]);
            var isReview = request.SystemPrompt.Contains("quality_score");
            var isEdit = items.OfType<JObject>().Any(item => item["target"] is not null);

            var answers = items.OfType<JObject>().Select(item => isReview
                ? new JObject { ["translation_id"] = item["id"], ["quality_score"] = 0.9 }
                : new JObject
                {
                    ["translation_id"] = item["id"],
                    ["translated_text"] = (isEdit ? "EDITED:" : "DE:") + item["source"]
                });

            return new JObject { ["translations"] = new JArray(answers) }.ToString(Formatting.None);
        }
    }

    // Starting a job may only call the synchronous client for language detection or a summary; these
    // tests avoid both, so any call is a bug.
    private sealed class SilentClient : ILlmClient
    {
        public LlmCapabilities Capabilities { get; } = new(SupportsStructuredOutput: false, ContextTokens: 200_000, MaxOutputTokens: 8_192);

        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken) =>
            throw new AssertFailedException("The synchronous client should not be called.");
    }

    private sealed class FakeBatchingApi : ISupportsBatching
    {
        public List<object> Submitted { get; } = [];

        public Task<BatchResponse> SendBatchRequestsAsync(List<object> requests)
        {
            Submitted.AddRange(requests);
            return Task.FromResult(new BatchResponse { Id = "msgbatch_fake", ProcessingStatus = "in_progress" });
        }

        public Task<List<BatchRequestDto>> GetBatchRequestsAsync(string batchId) => Task.FromResult(new List<BatchRequestDto>());

        public Task<BatchResponse> GetBatchStatusAsync(string batchId) => throw new NotSupportedException();

        public Task<List<BatchResponse>> ListBatches() => throw new NotSupportedException();
    }

    private sealed class ScriptedTranslationActions(InvocationContext context, IFileManagementClient files, IBatchLlmClient batch)
        : TranslationActions(context, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, AnthropicLlmSettings settings) => new SilentClient();

        protected override IBatchLlmClient CreateBatchClient(string? model, AnthropicLlmSettings settings) => batch;
    }

    private sealed class ScriptedEditActions(InvocationContext context, IFileManagementClient files, IBatchLlmClient batch)
        : EditActions(context, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, AnthropicLlmSettings settings) => new SilentClient();

        protected override IBatchLlmClient CreateBatchClient(string? model, AnthropicLlmSettings settings) => batch;
    }

    private sealed class ScriptedReviewActions(InvocationContext context, IFileManagementClient files, IBatchLlmClient batch)
        : ReviewActions(context, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, AnthropicLlmSettings settings) => new SilentClient();

        protected override IBatchLlmClient CreateBatchClient(string? model, AnthropicLlmSettings settings) => batch;
    }

    private sealed class ScriptedBackgroundActions(InvocationContext context, IFileManagementClient files, IBatchLlmClient batch)
        : BackgroundActions(context, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, AnthropicLlmSettings settings) => new SilentClient();

        protected override IBatchLlmClient CreateBatchClient(string? model, AnthropicLlmSettings settings) => batch;
    }
}
