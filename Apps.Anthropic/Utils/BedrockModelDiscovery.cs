using Apps.Anthropic.Models.Response;

namespace Apps.Anthropic.Utils;

internal sealed record BedrockInferenceProfile(
    string Id,
    string Name,
    string Status,
    IReadOnlyCollection<string> ModelArns);

internal static class BedrockModelDiscovery
{
    private const string ActiveStatus = "ACTIVE";

    public static List<ModelResponse> Merge(
        IEnumerable<ModelResponse> foundationModels,
        IEnumerable<BedrockInferenceProfile> inferenceProfiles)
    {
        var profileModels = inferenceProfiles
            .Where(IsActiveAnthropicProfile)
            .Select(profile => new ModelResponse(
                profile.Id,
                $"{profile.Name} (inference profile)"));

        return foundationModels
            .Concat(profileModels)
            .Where(model => !string.IsNullOrWhiteSpace(model.Id))
            .GroupBy(model => model.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(model => model.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsActiveAnthropicProfile(BedrockInferenceProfile profile)
    {
        if (!string.Equals(profile.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return ContainsAnthropicIdentifier(profile.Id)
               || ContainsAnthropicIdentifier(profile.Name)
               || profile.ModelArns.Any(ContainsAnthropicIdentifier);
    }

    private static bool ContainsAnthropicIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (value.Contains("anthropic", StringComparison.OrdinalIgnoreCase)
            || value.Contains("claude", StringComparison.OrdinalIgnoreCase));
}
