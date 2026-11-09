using Apps.Anthropic.Actions;
using Apps.Anthropic.Ai;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Request.Optional;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Tests.Anthropic.Ai;

[TestClass]
public class AiErrorHandlingTests
{
    private readonly InMemoryFileManagementClient _files = new();

    [TestMethod]
    public async Task TranslateText_GlossaryIsNotTbx_ThrowsMisconfigurationWithoutLibraryException()
    {
        var glossary = _files.Add("glossary.csv", "en;de\ncat;Katze");
        var actions = new FakeProviderTranslationActions(Context(), _files, new RecordingLlmClient(_ => "{}"));

        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() => actions.TranslateText(
            new ModelIdentifier { Model = "claude-sonnet-5" },
            new TranslateTextRequest { Text = "cat", SourceLanguage = "en", TargetLanguage = "de", Glossary = glossary },
            new OptionalSkillRequest()));

        StringAssert.Contains(exception.Message, "TBX");
        Assert.IsNull(exception.InnerException);
    }

    [TestMethod]
    public async Task Mapped_UnexpectedException_IsThrownAsApplicationExceptionWithSameMessage()
    {
        var exception = await Assert.ThrowsExactlyAsync<PluginApplicationException>(() =>
            AiAdapters.Mapped<int>(() => throw new InvalidOperationException("Unexpected provider answer")));

        Assert.AreEqual("Unexpected provider answer", exception.Message);
        Assert.IsNull(exception.InnerException);
    }

    [TestMethod]
    public async Task Mapped_PluginException_IsRethrownUnchanged()
    {
        var thrown = new PluginMisconfigurationException("Please specify the model in the input");

        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            AiAdapters.Mapped<int>(() => throw thrown));

        Assert.AreSame(thrown, exception);
    }

    private static InvocationContext Context() => new()
    {
        AuthenticationCredentialsProviders =
        [
            new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.AnthropicNative),
            new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused")
        ]
    };

    private sealed class FakeProviderTranslationActions(InvocationContext context, IFileManagementClient files, ILlmClient provider)
        : TranslationActions(context, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, AnthropicLlmSettings settings) => provider;
    }
}
