using System.Globalization;
using Blackbird.Applications.Sdk.Common.Exceptions;

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
}
