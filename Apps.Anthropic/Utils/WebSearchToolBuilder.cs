using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Anthropic.Utils;

public static class WebSearchToolBuilder
{
    public const string ToolType = "web_search_20250305";
    public const string ToolName = "web_search";

    public static Dictionary<string, object>? Build(CompletionRequest input)
    {
        if (input.EnableWebSearch != true)
        {
            return null;
        }

        var allowedDomains = NormalizeDomains(input.AllowedDomains);
        var blockedDomains = NormalizeDomains(input.BlockedDomains);
        Validate(input, allowedDomains, blockedDomains);

        var tool = new Dictionary<string, object>
        {
            ["type"] = ToolType,
            ["name"] = ToolName
        };

        if (input.MaxWebSearches.HasValue)
        {
            tool["max_uses"] = input.MaxWebSearches.Value;
        }

        if (allowedDomains.Count != 0)
        {
            tool["allowed_domains"] = allowedDomains;
        }

        if (blockedDomains.Count != 0)
        {
            tool["blocked_domains"] = blockedDomains;
        }

        var userLocation = BuildUserLocation(input);
        if (userLocation != null)
        {
            tool["user_location"] = userLocation;
        }

        return tool;
    }

    private static void Validate(CompletionRequest input, List<string> allowedDomains, List<string> blockedDomains)
    {
        if (input.MaxWebSearches is < 1)
        {
            throw new PluginMisconfigurationException("Max web searches must be greater than 0.");
        }

        if (allowedDomains.Count != 0 && blockedDomains.Count != 0)
        {
            throw new PluginMisconfigurationException(
                "Web search accepts either allowed domains or blocked domains, not both. Please fill in only one of them.");
        }
    }

    private static List<string> NormalizeDomains(IEnumerable<string>? domains) =>
        domains?
            .Where(domain => !string.IsNullOrWhiteSpace(domain))
            .Select(domain => domain.Trim()
                .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .TrimEnd('/'))
            .Where(domain => domain.Length != 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

    private static Dictionary<string, string>? BuildUserLocation(CompletionRequest input)
    {
        var location = new Dictionary<string, string>();
        AddIfPresent(location, "city", input.UserLocationCity);
        AddIfPresent(location, "region", input.UserLocationRegion);
        AddIfPresent(location, "country", input.UserLocationCountry);
        AddIfPresent(location, "timezone", input.UserLocationTimezone);

        if (location.Count == 0)
        {
            return null;
        }

        location["type"] = "approximate";
        return location;
    }

    private static void AddIfPresent(Dictionary<string, string> location, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            location[key] = value.Trim();
        }
    }
}
