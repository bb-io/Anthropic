namespace Apps.Anthropic.Utils;

public sealed record ModelCapabilities(int MaxOutputTokens, bool SupportsSamplingParameters)
{
    public static readonly ModelCapabilities Default = new(4096, true);
}

public static class ModelCatalog
{
    private static readonly Dictionary<string, ModelCapabilities> Models = new()
    {
        ["claude-opus-4-1-20250805"] = new(32000, true),
        ["claude-opus-4-1"] = new(32000, true),
        ["claude-opus-4-20250514"] = new(32000, true),
        ["claude-opus-4-0"] = new(32000, true),
        ["claude-sonnet-4-20250514"] = new(64000, true),
        ["claude-sonnet-4-0"] = new(64000, true),
        ["claude-sonnet-4-6"] = new(64000, true),
        ["claude-sonnet-5"] = new(128000, false),
        ["claude-3-7-sonnet-20250219"] = new(64000, true),
        ["claude-3-7-sonnet-latest"] = new(64000, true),
        ["claude-3-5-haiku-20241022"] = new(8192, true),
        ["claude-3-5-haiku-latest"] = new(8192, true),
        ["claude-3-haiku-20240307"] = new(4096, true),
    };

    public static ModelCapabilities GetCapabilities(string? modelName)
    {
        if (string.IsNullOrEmpty(modelName))
        {
            return ModelCapabilities.Default;
        }

        if (Models.TryGetValue(modelName, out var capabilities))
        {
            return capabilities;
        }

        // Unlisted model IDs (e.g. dated snapshots not yet added above): the Sonnet 5 family
        // rejects non-default sampling parameters regardless of the exact suffix, so fall back
        // to a substring match for that instead of silently allowing them.
        var supportsSamplingParameters = !modelName.Contains("claude-sonnet-5", StringComparison.OrdinalIgnoreCase);

        return new ModelCapabilities(ModelCapabilities.Default.MaxOutputTokens, supportsSamplingParameters);
    }

    public static int GetMaxOutputTokens(string? modelName) => GetCapabilities(modelName).MaxOutputTokens;

    public static bool SupportsSamplingParameters(string? modelName) => GetCapabilities(modelName).SupportsSamplingParameters;
}
