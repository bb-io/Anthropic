namespace TestPlugin.Dtos.TypesActions;

public class IEnumerableIssueObject
{
    public IEnumerable<IEnumerableIssueInnerObject> InnerObjects { get; set; }
}

public class IEnumerableIssueInnerObject
{
    public string Test { get; set; }
}
