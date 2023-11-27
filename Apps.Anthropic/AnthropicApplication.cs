using Blackbird.Applications.Sdk.Common;

namespace Apps.Anthropic;

public class AnthropicApplication : IApplication
{
    public string Name
    {
        get => "Anthropic";
        set { }
    }

    public T GetInstance<T>()
    {
        throw new NotImplementedException();
    }
}