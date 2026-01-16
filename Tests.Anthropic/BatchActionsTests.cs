using Tests.Anthropic.Base;
using Apps.Anthropic.Actions;
using Apps.Anthropic.Constants;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Anthropic;

[TestClass]
public class BatchActionsTests : TestBaseMultipleConnections
{
    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task ProcessXliffFileAsync_ValidXliff_ShouldCreateBatch(InvocationContext context)
    {
        // Arrange
        var actions = new BatchActions(context, FileManager);

        // Act
        var batch = await actions.ProcessXliffFileAsync(new()
        {
            Model = "claude-3-5-sonnet-20240620",
            File = new()
            {
                Name = "simple.xliff",
                ContentType = "text/xml"
            }
        });

        // Assert
        PrintResult(batch);
        Assert.IsNotNull(batch.Id);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task GetBatchResults_ValidXliff_ShouldReturnValidXliff(InvocationContext context)
    {
        // Arrange
        var actions = new BatchActions(context, FileManager);

        // Act
        var batch = await actions.GetBatchResultsAsync(new()
        {
            OriginalXliff = new()
            {
                Name = "test.xlf",
                ContentType = "text/xml"
            },
            BatchId = "msgbatch_01KM4XQE7PMGakAAViDkThCG"
        });

        // Assert
        PrintResult(batch);
        Assert.IsNotNull(batch.File.Name);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task GetBatchResults_HasXmlTags_ShouldReturnValidXliff(InvocationContext context)
    {
        // Arrange
        var actions = new BatchActions(context, FileManager);

        // Act
        var batch = await actions.GetBatchResultsAsync(new()
        {
            OriginalXliff = new()
            {
                Name = "simple_with_html.xlf",
                ContentType = "text/xml"
            },
            BatchId = "msgbatch_016Jc3Au7guFtBDfTbQi1baR"
        });

        // Assert
        PrintResult(batch);
        Assert.IsNotNull(batch.File.Name);
    }
}