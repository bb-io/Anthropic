using Apps.Anthropic.Actions;
using FluentAssertions;
using Newtonsoft.Json;
using Tests.Anthropic.Base;

namespace Tests.Anthropic;

[TestClass]
public class CompletionActionsTests : TestBase
{
    [TestMethod]
    public async Task CreateCompletion_HelloWorldPrompt_ShouldBeSuccessful()
    {
        var actions = new CompletionActions(InvocationContext, FileManager);
        var response = await actions.CreateCompletion(new()
        {
            Prompt = "Hello, world",
            Model = "claude-3-5-haiku-20241022"
        }, new());

        response.Text.Should().NotBeNullOrEmpty();
        response.Usage.InputTokens.Should().NotBe(0);

        Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
    }
}