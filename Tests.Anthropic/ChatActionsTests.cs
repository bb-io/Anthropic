using Apps.Anthropic.Actions;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.Anthropic.Ai;
using Tests.Anthropic.Base;

namespace Tests.Anthropic;

[TestClass]
public class ChatActionsTests : TestBaseMultipleConnections
{
    private const string CheapWebSearchModel = "claude-haiku-4-5";

    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task CreateCompletion_ReturnsValidChatResponse(InvocationContext context)
    {
        var actions = new ChatActions(context, FileManager);
        var modelId = new ModelIdentifier { Model = "claude-sonnet-4-6" };
        var completionRequest = new CompletionRequest
        {
            Prompt = "Describe this skill",
            SkillId = "pdf"
        };
        var glossary = new GlossaryRequest { };

        var result = await actions.CreateCompletion(modelId, completionRequest, glossary);

        PrintResult(result);
        Assert.IsNotNull(result.Text);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task CreateCompletion_WebSearchWithAllowedDomain_ReturnsAnswerWithSourcesFromThatDomain(InvocationContext context)
    {
        var actions = new ChatActions(context, FileManager);

        var result = await actions.CreateCompletion(
            new ModelIdentifier { Model = CheapWebSearchModel },
            new CompletionRequest
            {
                Prompt = "Search the web: what is the latest stable .NET release? Answer in one short sentence.",
                MaxTokensToSample = 1024,
                EnableWebSearch = true,
                MaxWebSearches = 2,
                AllowedDomains = ["https://learn.microsoft.com/"],
                UserLocationCountry = "US"
            },
            new GlossaryRequest());

        PrintResult(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Text));
        Assert.IsNotEmpty(result.Sources);
        Assert.IsTrue(result.Sources.All(url => new Uri(url).Host == "learn.microsoft.com"), string.Join(", ", result.Sources));
        Assert.IsTrue(result.Citations.All(citation => !string.IsNullOrWhiteSpace(citation.Url)));
        Assert.AreNotEqual("pause_turn", result.StopReason);
        Assert.IsGreaterThan(0, result.Usage.InputTokens);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task CreateCompletion_WithoutWebSearch_ReturnsNoSources(InvocationContext context)
    {
        var actions = new ChatActions(context, FileManager);

        var result = await actions.CreateCompletion(
            new ModelIdentifier { Model = CheapWebSearchModel },
            new CompletionRequest { Prompt = "Reply with the single word: pong", MaxTokensToSample = 16 },
            new GlossaryRequest());

        PrintResult(result);
        StringAssert.Contains(result.Text.ToLowerInvariant(), "pong");
        Assert.IsEmpty(result.Sources);
        Assert.IsEmpty(result.Citations);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task CreateCompletion_FilteredGlossary_UsesGlossaryTermFromPrompt(InvocationContext context)
    {
        var files = new InMemoryFileManagementClient();
        var actions = new ChatActions(context, files);

        var result = await actions.CreateCompletion(
            new ModelIdentifier { Model = CheapWebSearchModel },
            new CompletionRequest
            {
                Prompt = "Translate into German using the glossary. Reply with the translation only: The cat sleeps.",
                MaxTokensToSample = 64
            },
            new GlossaryRequest { Glossary = files.Add("glossary.tbx", ChatOfflineTests.TwoEntryGlossary) });

        PrintResult(result);
        StringAssert.Contains(result.Text, "Katze");
    }
}
