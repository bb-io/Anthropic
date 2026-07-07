using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Transformations;

namespace Apps.Anthropic.Extensions;

public static class StreamExtensions
{
    public static Transformation LoadTransformation(this Stream inputFileStream, string fileName)
    {
        var transformationLoadResult = Transformation.Load(inputFileStream, fileName);
        return !transformationLoadResult.Success 
            ? throw new PluginMisconfigurationException(transformationLoadResult.Error) 
            : transformationLoadResult.Value;
    }
}