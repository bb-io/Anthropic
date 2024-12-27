using Apps.Anthropic.DataSourceHandlers;
using FluentAssertions;
using Newtonsoft.Json;
using Tests.Anthropic.Base;

namespace Tests.Anthropic;

[TestClass]
public class BatchDataSourceTests : TestBase
{
    [TestMethod]
    public async Task GetData_WithourSearchParameter_ShouldNotNullCollection()
    {
        var dataSource = new BatchDataSource(InvocationContext);
        var data = await dataSource.GetDataAsync(new(), default);

        data.Should().NotBeNull();

        Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
    }
}