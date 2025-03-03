using Apps.Anthropic.Actions;
using FluentAssertions;
using Newtonsoft.Json;
using Tests.Anthropic.Base;

namespace Tests.Anthropic;

[TestClass]
public class BatchActionsTests : TestBase
{
    [TestMethod]
    public async Task ProcessXliffFile_ValidXliff_ShouldCreateBatch()
    {
        var actions = new BatchActions(InvocationContext, FileManager);
        var batch = await actions.ProcessXliffFileAsync(new()
        {
            Model = "claude-3-5-sonnet-20240620",
            File = new()
            {
                Name = "test.xlf",
                ContentType = "text/xml"
            }
        });

        batch.Id.Should().NotBeNullOrEmpty();

        Console.WriteLine(JsonConvert.SerializeObject(batch, Formatting.Indented));
    }
    
    
    [TestMethod]
    public async Task GetBatchResults_ValidXliff_ShouldReturnValidXliff()
    {
        var actions = new BatchActions(InvocationContext, FileManager);
        var batch = await actions.GetBatchResultsAsync(new()
        {
            OriginalXliff = new()
            {
                Name = "test.xlf",
                ContentType = "text/xml"
            }, 
            BatchId = "msgbatch_01KM4XQE7PMGakAAViDkThCG"
        });

        batch.File.Name.Should().NotBeNullOrEmpty();
        Console.WriteLine(JsonConvert.SerializeObject(batch, Formatting.Indented));
    }

    [TestMethod]
    public async Task GetBatchResults_HasXmlTags_ShouldReturnValidXliff()
    {
        var actions = new BatchActions(InvocationContext, FileManager);
        var batch = await actions.GetBatchResultsAsync(new()
        {
            OriginalXliff = new()
            {
                Name = "simple_with_html.xlf",
                ContentType = "text/xml"
            },
            BatchId = "msgbatch_016Jc3Au7guFtBDfTbQi1baR"
        });

        batch.File.Name.Should().NotBeNullOrEmpty();
        Console.WriteLine(JsonConvert.SerializeObject(batch, Formatting.Indented));
    }
}