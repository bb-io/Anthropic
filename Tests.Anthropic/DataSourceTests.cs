using Apps.Anthropic.DataSourceHandlers;
using FluentAssertions;
using Newtonsoft.Json;
using Tests.Anthropic.Base;

namespace Tests.Anthropic;

[TestClass]
public class DataSourceTests : TestBase
{
    [TestMethod]
    public async Task BatchDataSource_GetData_WithoutSearchParameter_ShouldNotNullCollection()
    {
        var dataSource = new BatchDataSource(InvocationContext);
        var data = await dataSource.GetDataAsync(new(), default);

        data.Should().NotBeNull();

        Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
    }

    [TestMethod]
    public async Task Models_ShouldNotNullCollection()
    {
        var dataSource = new ModelDataSource(InvocationContext);
        var data = await dataSource.GetDataAsync(new(), default);

        data.Should().NotBeNull();

        Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
    }

}