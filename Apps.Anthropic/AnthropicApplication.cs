using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Metadata;

namespace Apps.Anthropic;

public class AnthropicApplication : IApplication, ICategoryProvider
{
    public IEnumerable<ApplicationCategory> Categories
    {
        get => [ApplicationCategory.ArtificialIntelligence];
        set { }
    }
    
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