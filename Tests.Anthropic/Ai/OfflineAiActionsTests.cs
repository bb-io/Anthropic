using Apps.Anthropic.Actions;
using Apps.Anthropic.Ai;
using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Request.Optional;
using Apps.Anthropic.Models.Response;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Tests.Anthropic.Ai;

// Offline, no API spend: the provider is replaced through the actions' CreateLlmClient hook.
[TestClass]
public class OfflineAiActionsTests
{
    private const string TwoUntranslatedSegments = """
        <?xml version="1.0" encoding="utf-8"?>
        <xliff version="2.0" srcLang="en" trgLang="de" xmlns="urn:oasis:names:tc:xliff:document:2.0">
          <file id="f1" original="test.xlf">
            <unit id="u1"><segment id="s1"><source>One</source></segment></unit>
            <unit id="u2"><segment id="s2"><source>Two</source></segment></unit>
          </file>
        </xliff>
        """;

    private readonly InMemoryFileManagementClient _files = new();
    private readonly List<string> _loggedWarnings = [];

    // Warnings are no longer an output: the library logs them, and they must reach InvocationContext.Logger.
    [TestMethod]
    public async Task Translate_UnusableAnswers_LogsWarningsAndLeavesSegmentsUntouched()
    {
        var file = _files.Add("broken.xlf", TwoUntranslatedSegments);
        var actions = new FakeProviderTranslationActions(Context(), _files, new RecordingLlmClient(_ => "not json"));

        var result = await actions.Translate(
            new ModelIdentifier { Model = "claude-sonnet-5" },
            new TranslateContentRequest { File = file, TargetLanguage = "de", GenerateDocumentSummary = false },
            new OptionalSkillRequest());

        Assert.AreEqual(0, result.UpdatedSegmentsCount);
        Assert.IsNotEmpty(_loggedWarnings);
        Assert.IsTrue(_loggedWarnings.All(warning => warning.Contains("[Anthropic]")), string.Join(Environment.NewLine, _loggedWarnings));
    }

    // A text action has nothing to return when the model never answers usably, so it throws with the reasons.
    [TestMethod]
    public async Task TranslateText_UnusableAnswers_LogsWarningsAndThrows()
    {
        var actions = new FakeProviderTranslationActions(Context(), _files, new RecordingLlmClient(_ => "not json"));

        var exception = await Assert.ThrowsExactlyAsync<PluginApplicationException>(() => actions.TranslateText(
            new ModelIdentifier { Model = "claude-sonnet-5" },
            new TranslateTextRequest { Text = "Hello", SourceLanguage = "en", TargetLanguage = "de" },
            new OptionalSkillRequest()));

        Assert.IsNotEmpty(_loggedWarnings);
        Assert.IsTrue(_loggedWarnings.All(warning => exception.Message.Split(Environment.NewLine).Any(warning.Contains)),
            $"logged: {string.Join(" | ", _loggedWarnings)}; thrown: {exception.Message}");
    }

    // The "System prompt" input sets the model's role; the library's response contract must still be sent,
    // or no answer could be mapped back to its segment.
    [TestMethod]
    public async Task Translate_CustomSystemPrompt_IsPrependedAndKeepsResponseContract()
    {
        const string customPrompt = "You translate medical device documentation for clinicians.";
        var file = _files.Add("custom.xlf", TwoUntranslatedSegments);
        var provider = new RecordingLlmClient(_ => "not json");
        var actions = new FakeProviderTranslationActions(Context(), _files, provider);

        await actions.Translate(
            new ModelIdentifier { Model = "claude-sonnet-5" },
            new TranslateContentRequest { File = file, TargetLanguage = "de", SystemPrompt = customPrompt, GenerateDocumentSummary = false },
            new OptionalSkillRequest());

        Assert.IsNotEmpty(provider.Requests);
        Assert.IsTrue(provider.Requests.All(request => request.SystemPrompt.StartsWith(customPrompt)));
        Assert.IsTrue(provider.Requests.All(request => request.SystemPrompt.Contains("### OUTPUT FORMAT (STRICT)")));
    }

    // Truncation recovery depends on the provider's stop reason; usage keeps Anthropic's cache counters.
    [TestMethod]
    public async Task AnthropicLlmClient_MaxTokensStop_IsReportedAsTruncatedAndUsageIsKept()
    {
        var provider = new FixedAnthropicClient(new ResponseMessage
        {
            Text = "{\"translations\":[",
            StopReason = "max_tokens",
            Usage = new UsageResponse { InputTokens = 100, OutputTokens = 50, CacheReadInputTokens = 7 }
        });
        var client = new AnthropicLlmClient(provider, "claude-haiku-4-5-20251001", new AnthropicLlmSettings(MaxTokens: 1_000));

        var response = await client.CompleteAsync(new LlmRequest("system", "user"), CancellationToken.None);

        Assert.AreEqual(FinishReason.Truncated, response.Finish);
        Assert.AreEqual(new LlmUsage(100, 50), response.Usage);
        Assert.AreEqual(1_000, client.Capabilities.MaxOutputTokens);
        Assert.AreEqual(7, client.Usage.CacheReadInputTokens);
        Assert.AreEqual(1_000, provider.LastRequest!.MaxTokens);
    }

    [TestMethod]
    public void AnthropicLlmClient_NoMaxTokensInput_UsesModelOutputLimit()
    {
        var client = new AnthropicLlmClient(new FixedAnthropicClient(new ResponseMessage()), "claude-haiku-5-5", new AnthropicLlmSettings());

        Assert.AreEqual(128_000, client.Capabilities.MaxOutputTokens);
    }

    private InvocationContext Context() => new()
    {
        AuthenticationCredentialsProviders =
        [
            new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.AnthropicNative),
            new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused")
        ],
        Logger = new Logger("Debug") { LogWarning = (message, _) => _loggedWarnings.Add(message ?? string.Empty) }
    };

    private sealed class FakeProviderTranslationActions(InvocationContext context, IFileManagementClient files, ILlmClient provider)
        : TranslationActions(context, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, AnthropicLlmSettings settings) => provider;
    }

    private sealed class FixedAnthropicClient(ResponseMessage response) : IAnthropicClient
    {
        public MessageRequest? LastRequest { get; private set; }

        public Task<ResponseMessage> ExecuteChat(MessageRequest message)
        {
            LastRequest = message;
            return Task.FromResult(response);
        }

        public Task<ConnectionValidationResponse> ValidateConnection() => throw new NotSupportedException();

        public Task<List<ModelResponse>> ListModels() => throw new NotSupportedException();
    }
}
