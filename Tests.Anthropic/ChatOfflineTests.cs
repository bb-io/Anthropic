using Apps.Anthropic.Api.Anthropic;
using Apps.Anthropic.Api.Vertex;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tests.Anthropic.Ai;

namespace Tests.Anthropic;

[TestClass]
public class ChatOfflineTests
{
    internal const string TwoEntryGlossary = """
        <?xml version="1.0" encoding="UTF-8"?>
        <tbx xml:lang="en" type="TBX" style="dct" xmlns="urn:iso:std:iso:30042:ed-2">
          <tbxHeader>
            <fileDesc>
              <titleStmt><title>Test glossary</title></titleStmt>
              <sourceDesc><p>Test glossary</p></sourceDesc>
            </fileDesc>
          </tbxHeader>
          <text>
            <body>
              <conceptEntry id="c1">
                <langSec xml:lang="en"><termSec><term>cat</term></termSec></langSec>
                <langSec xml:lang="de"><termSec><term>Katze</term></termSec></langSec>
              </conceptEntry>
              <conceptEntry id="c2">
                <langSec xml:lang="en"><termSec><term>spaceship</term></termSec></langSec>
                <langSec xml:lang="de"><termSec><term>Raumschiff</term></termSec></langSec>
              </conceptEntry>
            </body>
          </text>
        </tbx>
        """;

    private const string PausedSearchResponse = """
        {
          "content": [
            { "type": "text", "text": "Let me search. " },
            { "type": "server_tool_use", "id": "srvtoolu_1", "name": "web_search", "input": { "query": "answer" } },
            {
              "type": "web_search_tool_result",
              "tool_use_id": "srvtoolu_1",
              "content": [
                { "type": "web_search_result", "url": "https://example.com/a", "title": "Page A", "encrypted_content": "encrypted-1" },
                { "type": "web_search_result", "url": "https://example.com/b", "title": "Page B", "encrypted_content": "encrypted-2" }
              ]
            }
          ],
          "stop_reason": "pause_turn",
          "usage": { "input_tokens": 10, "output_tokens": 2 }
        }
        """;

    private const string FinalAnswerResponse = """
        {
          "content": [
            {
              "type": "text",
              "text": "The answer is 42.",
              "citations": [
                { "type": "web_search_result_location", "url": "https://example.com/a", "title": "Page A", "cited_text": "42", "encrypted_index": "index-1" }
              ]
            }
          ],
          "stop_reason": "end_turn",
          "usage": { "input_tokens": 20, "output_tokens": 5 }
        }
        """;

    private const string SearchErrorResponse = """
        {
          "content": [
            { "type": "server_tool_use", "id": "srvtoolu_1", "name": "web_search", "input": { "query": "answer" } },
            {
              "type": "web_search_tool_result",
              "tool_use_id": "srvtoolu_1",
              "content": { "type": "web_search_tool_result_error", "error_code": "max_uses_exceeded" }
            },
            { "type": "text", "text": "I could not search." }
          ],
          "stop_reason": "end_turn",
          "usage": { "input_tokens": 1, "output_tokens": 1 }
        }
        """;

    private readonly InMemoryFileManagementClient _files = new();

    [TestMethod]
    public async Task GenerateChatMessages_GlossaryByDefault_IncludesOnlyEntriesFoundInPrompt()
    {
        var glossary = new GlossaryRequest { Glossary = _files.Add("glossary.tbx", TwoEntryGlossary) };
        var utilities = new AiUtilities(AnthropicContext(), _files);

        var messages = await utilities.GenerateChatMessages(
            new CompletionRequest { Prompt = "Translate 'The cat sleeps' to German" },
            glossary);

        var prompt = messages.Single().Content;
        StringAssert.Contains(prompt, "Katze");
        Assert.DoesNotContain("Raumschiff", prompt);
    }

    [TestMethod]
    public async Task GenerateChatMessages_GlossaryFilterDisabled_IncludesWholeGlossary()
    {
        var glossary = new GlossaryRequest
        {
            Glossary = _files.Add("glossary.tbx", TwoEntryGlossary),
            FilterGlossary = false
        };
        var utilities = new AiUtilities(AnthropicContext(), _files);

        var messages = await utilities.GenerateChatMessages(
            new CompletionRequest { Prompt = "Translate 'The cat sleeps' to German" },
            glossary);

        var prompt = messages.Single().Content;
        StringAssert.Contains(prompt, "Katze");
        StringAssert.Contains(prompt, "Raumschiff");
    }

    [TestMethod]
    public async Task GenerateChatMessages_GlossaryTermOnlyInFileText_IsIncluded()
    {
        var glossary = new GlossaryRequest { Glossary = _files.Add("glossary.tbx", TwoEntryGlossary) };
        var utilities = new AiUtilities(AnthropicContext(), _files);

        var messages = await utilities.GenerateChatMessages(
            new CompletionRequest { Prompt = "Translate the file to German" },
            glossary,
            "The spaceship lands.");

        Assert.HasCount(2, messages);
        StringAssert.Contains(messages[0].Content, "Raumschiff");
        Assert.DoesNotContain("Katze", messages[0].Content);
        StringAssert.Contains(messages[1].Content, "The spaceship lands.");
    }

    [TestMethod]
    public async Task GenerateChatMessages_NoGlossaryTermInPrompt_SendsPromptWithoutGlossary()
    {
        var glossary = new GlossaryRequest { Glossary = _files.Add("glossary.tbx", TwoEntryGlossary) };
        var utilities = new AiUtilities(AnthropicContext(), _files);

        var messages = await utilities.GenerateChatMessages(new CompletionRequest { Prompt = "Hello" }, glossary);

        Assert.AreEqual("Hello", messages.Single().Content);
    }

    [TestMethod]
    public void WebSearchToolBuilder_WebSearchNotEnabled_ReturnsNull()
    {
        Assert.IsNull(WebSearchToolBuilder.Build(new CompletionRequest { Prompt = "Hi", AllowedDomains = ["example.com"] }));
    }

    [TestMethod]
    public void WebSearchToolBuilder_AllOptions_BuildsAnthropicWebSearchTool()
    {
        var tool = WebSearchToolBuilder.Build(new CompletionRequest
        {
            Prompt = "Hi",
            EnableWebSearch = true,
            MaxWebSearches = 3,
            AllowedDomains = ["https://example.com/", " docs.example.org ", "", "EXAMPLE.com"],
            UserLocationCity = "Kyiv",
            UserLocationCountry = "UA",
            UserLocationTimezone = "Europe/Kyiv"
        });

        var json = JObject.Parse(JsonConvert.SerializeObject(tool));
        Assert.AreEqual("web_search_20250305", json["type"]?.Value<string>());
        Assert.AreEqual("web_search", json["name"]?.Value<string>());
        Assert.AreEqual(3, json["max_uses"]?.Value<int>());
        CollectionAssert.AreEqual(
            new[] { "example.com", "docs.example.org" },
            json["allowed_domains"]!.Values<string>().ToArray());
        Assert.IsNull(json["blocked_domains"]);
        Assert.AreEqual("approximate", json["user_location"]?["type"]?.Value<string>());
        Assert.AreEqual("Kyiv", json["user_location"]?["city"]?.Value<string>());
        Assert.AreEqual("UA", json["user_location"]?["country"]?.Value<string>());
        Assert.AreEqual("Europe/Kyiv", json["user_location"]?["timezone"]?.Value<string>());
        Assert.IsNull(json["user_location"]?["region"]);
    }

    [TestMethod]
    public void WebSearchToolBuilder_EnabledWithoutOptions_SendsOnlyTypeAndName()
    {
        var tool = WebSearchToolBuilder.Build(new CompletionRequest { Prompt = "Hi", EnableWebSearch = true });

        CollectionAssert.AreEquivalent(new[] { "type", "name" }, tool!.Keys.ToArray());
    }

    [TestMethod]
    public void WebSearchToolBuilder_AllowedAndBlockedDomains_Throws()
    {
        Assert.ThrowsExactly<PluginMisconfigurationException>(() => WebSearchToolBuilder.Build(new CompletionRequest
        {
            Prompt = "Hi",
            EnableWebSearch = true,
            AllowedDomains = ["example.com"],
            BlockedDomains = ["example.org"]
        }));
    }

    [TestMethod]
    public void WebSearchToolBuilder_ZeroMaxWebSearches_Throws()
    {
        Assert.ThrowsExactly<PluginMisconfigurationException>(() => WebSearchToolBuilder.Build(new CompletionRequest
        {
            Prompt = "Hi",
            EnableWebSearch = true,
            MaxWebSearches = 0
        }));
    }

    [TestMethod]
    public async Task BuildMessageRequest_WebSearchOnAnthropicConnection_AddsWebSearchTool()
    {
        var utilities = new AiUtilities(AnthropicContext(), _files);

        var request = await utilities.BuildMessageRequest(
            new ModelIdentifier { Model = "claude-sonnet-4-6" },
            new CompletionRequest { Prompt = "Latest news?", EnableWebSearch = true },
            new GlossaryRequest());

        Assert.AreEqual("web_search", request.WebSearchTool?["name"]);
    }

    [TestMethod]
    public async Task BuildMessageRequest_WebSearchOnBedrockConnection_Throws()
    {
        var utilities = new AiUtilities(BedrockContext(), _files);

        await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() => utilities.BuildMessageRequest(
            new ModelIdentifier { Model = "anthropic.claude-sonnet-4-6" },
            new CompletionRequest { Prompt = "Latest news?", EnableWebSearch = true },
            new GlossaryRequest()));
    }

    [TestMethod]
    public void AddServerTools_SkillAndWebSearch_SendsBothTools()
    {
        var message = new MessageRequest
        {
            SkillId = "pdf",
            WebSearchTool = WebSearchToolBuilder.Build(new CompletionRequest { Prompt = "Hi", EnableWebSearch = true })
        };
        var payload = new Dictionary<string, object?>();

        BaseAnthropicClient.AddServerTools(payload, message, supportsSkills: true);

        var json = JObject.Parse(JsonConvert.SerializeObject(payload));
        CollectionAssert.AreEqual(
            new[] { "code_execution", "web_search" },
            json["tools"]!.Select(tool => tool["name"]!.Value<string>()).ToArray());
        Assert.IsNotNull(json["container"]);
    }

    [TestMethod]
    public void AddServerTools_NoServerTools_LeavesPayloadWithoutTools()
    {
        var payload = new Dictionary<string, object?>();

        BaseAnthropicClient.AddServerTools(payload, new MessageRequest(), supportsSkills: true);

        Assert.IsFalse(payload.ContainsKey("tools"));
    }

    [TestMethod]
    public void VertexBuildMessagePayload_WebSearch_SendsWebSearchTool()
    {
        var request = new MessageRequest
        {
            Model = "claude-sonnet-4-6",
            MaxTokens = 100,
            System = string.Empty,
            StopSequences = [],
            Messages = [new() { Role = "user", Content = "Latest news?" }],
            WebSearchTool = WebSearchToolBuilder.Build(new CompletionRequest { Prompt = "Hi", EnableWebSearch = true })
        };

        var json = JObject.Parse(JsonConvert.SerializeObject(GoogleVertexRestClient.BuildMessagePayload(request)));

        Assert.AreEqual("web_search_20250305", json["tools"]?[0]?["type"]?.Value<string>());
    }

    [TestMethod]
    public async Task PausedTurnContinuation_PauseTurnWithWebSearch_ResendsPausedContentAndMergesResponses()
    {
        var conversation = new List<object> { new { role = "user", content = "Latest news?" } };
        var sentConversations = new List<JArray>();
        var responses = new Queue<string>([PausedSearchResponse, FinalAnswerResponse]);

        var result = await PausedTurnContinuation.ExecuteAsync(WebSearchMessage(), conversation, () =>
        {
            sentConversations.Add(JArray.Parse(JsonConvert.SerializeObject(conversation)));
            return Task.FromResult(responses.Dequeue());
        });

        Assert.HasCount(2, sentConversations);
        var resentAssistantMessage = sentConversations[1][1];
        Assert.AreEqual("assistant", resentAssistantMessage["role"]?.Value<string>());
        Assert.AreEqual(
            "encrypted-1",
            resentAssistantMessage["content"]?[2]?["content"]?[0]?["encrypted_content"]?.Value<string>());
        Assert.AreEqual("Let me search. The answer is 42.", result.Text);
        Assert.AreEqual("end_turn", result.StopReason);
        Assert.AreEqual(30, result.Usage.InputTokens);
        Assert.AreEqual(7, result.Usage.OutputTokens);
        CollectionAssert.AreEqual(new[] { "https://example.com/a", "https://example.com/b" }, result.Sources);
        Assert.HasCount(1, result.Citations);
        Assert.AreEqual("https://example.com/a", result.Citations[0].Url);
        Assert.AreEqual("Page A", result.Citations[0].Title);
    }

    [TestMethod]
    public async Task PausedTurnContinuation_SearchErrorResult_ReturnsTextWithoutSources()
    {
        var result = await PausedTurnContinuation.ExecuteAsync(
            WebSearchMessage(),
            [],
            () => Task.FromResult(SearchErrorResponse));

        Assert.AreEqual("I could not search.", result.Text);
        Assert.IsEmpty(result.Sources);
        Assert.IsEmpty(result.Citations);
    }

    [TestMethod]
    public async Task PausedTurnContinuation_PauseTurnWithoutWebSearch_ReturnsFirstResponse()
    {
        var calls = 0;

        var result = await PausedTurnContinuation.ExecuteAsync(new MessageRequest(), [], () =>
        {
            calls++;
            return Task.FromResult(PausedSearchResponse);
        });

        Assert.AreEqual(1, calls);
        Assert.AreEqual("pause_turn", result.StopReason);
    }

    [TestMethod]
    public async Task PausedTurnContinuation_AlwaysPaused_StopsAfterMaxContinuations()
    {
        var calls = 0;

        var result = await PausedTurnContinuation.ExecuteAsync(WebSearchMessage(), [], () =>
        {
            calls++;
            return Task.FromResult(PausedSearchResponse);
        });

        Assert.AreEqual(PausedTurnContinuation.MaxContinuations + 1, calls);
        Assert.AreEqual("pause_turn", result.StopReason);
    }

    private static MessageRequest WebSearchMessage() => new()
    {
        WebSearchTool = WebSearchToolBuilder.Build(new CompletionRequest { Prompt = "Hi", EnableWebSearch = true })
    };

    private static InvocationContext AnthropicContext() => new()
    {
        AuthenticationCredentialsProviders =
        [
            new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.AnthropicNative),
            new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused")
        ]
    };

    private static InvocationContext BedrockContext() => new()
    {
        AuthenticationCredentialsProviders =
        [
            new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.BedrockApiKey),
            new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused"),
            new AuthenticationCredentialsProvider(CredNames.Region, "us-east-1")
        ]
    };
}
