using System.Globalization;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Annotation;

namespace Apps.Anthropic.Extensions;

public static class StringExtensions
{
    public static float? ToOptionalFloat(this string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        throw new PluginMisconfigurationException($"The '{fieldName}' value must be a valid number.");
    }
    
    public static int CountWords(this IEnumerable<LineElement> line)
    {
        return string.Concat(line.Where(IsText).Select(element => element.Value)).CountWords();
    }

    public static int CountWords(this string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Count(token => token.Any(char.IsLetterOrDigit));
    }

    private static bool IsText(LineElement element) => element is not (InlineTag or AnnotationStart or AnnotationEnd);
}
