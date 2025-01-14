using Apps.Anthropic.Actions;
using Apps.Anthropic.Models.Request;
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


    [TestMethod]
    public async Task GetQualityScores_IsNotNull()
    {
        var action = new CompletionActions(InvocationContext, FileManager);

        var input1 = new ProcessXliffRequest 
        {
            Xliff= new()
            {
                Name = "translated_anthropic.xliff",
                ContentType = "text/xml"
            },
            Model = "claude-3-5-sonnet-20240620",
            Prompt = "",
            SystemPrompt= "You are a professional translator."
        };
        var input2 = new GlossaryRequest { };


        var response = action.GetQualityScores(input1, input2);

        Console.WriteLine($"Response: {response.Result.AverageScore}");
    }
}