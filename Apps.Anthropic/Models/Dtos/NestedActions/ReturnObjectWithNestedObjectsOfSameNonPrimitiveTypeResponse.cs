namespace TestPlugin.Dtos.NestedActions;

public class ReturnObjectWithNestedObjectsOfSameNonPrimitiveTypeResponse
{
    public TestType Test1 { get; set; }
    public TestType Test2 { get; set; }
    public TestType Test3 { get; set; }
}

public class TestType
{
    public string Id { get; set; }
    public string Name { get; set; }
}