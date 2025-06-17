namespace TestPlugin.Dtos.NestedActions;

public class ReturnObjectWithNestedObjectsWithSameBaseTypeResponse
{
    public TestType Test { get; set; }
    public TestTypeFirstDerived FirstDerivedTest { get; set; }
    public TestTypeSecondDerived SecondDerivedTest { get; set; }
}

public class TestTypeFirstDerived : TestType 
{
}

public class TestTypeSecondDerived : TestType 
{
    public string AdditionalProperty { get; set; }
}