using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Content;
using Blackbird.Filters.Transformations;

namespace Apps.Anthropic.Extensions;

public static class TransformationExtensions
{
    public static CodedContent LoadSource(this Transformation transformation)
    {
        var contentSourceLoadResult = transformation.Source();
        return !contentSourceLoadResult.Success 
            ? throw new PluginMisconfigurationException(contentSourceLoadResult.Error) 
            : contentSourceLoadResult.Value;
    }
    
    public static CodedContent LoadTarget(this Transformation transformation)
    {
        var contentSourceLoadResult = transformation.Target();
        return !contentSourceLoadResult.Success 
            ? throw new PluginMisconfigurationException(contentSourceLoadResult.Error) 
            : contentSourceLoadResult.Value;
    }
}