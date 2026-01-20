using Tests.Anthropic.Base;
using Apps.Anthropic.Constants;
using Apps.Anthropic.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Anthropic;

[TestClass]
public class DataSourceTests : TestBaseMultipleConnections
{
    [TestMethod, ContextDataSource(ConnectionTypes.AnthropicNative)]
    public async Task BatchDataSource_WithoutSearchParameter_ShouldNotNullCollection(InvocationContext context)
    {
        // Arrange
        var dataSource = new BatchDataSource(context);

        // Act
        var data = await dataSource.GetDataAsync(new(), default);

        // Assert
        PrintDataHandlerResult(data);
        Assert.IsNotNull(data);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.BedrockApiKey)]
    public async Task ModelDataSource_ShouldNotNullCollection(InvocationContext context)
    {
        // Arrange
        var dataSource = new ModelDataSource(context);

        // Act
        var data = await dataSource.GetDataAsync(new(), default);

        // Assert
        PrintDataHandlerResult(data);
        Assert.IsNotNull(data);
    }
}