namespace Apps.Anthropic.Utils;

public static class ModelCapabilityService
{
    public static bool SupportsSamplingParameters(string? modelName)
        => !IsClaudeSonnet5(modelName);

    private static bool IsClaudeSonnet5(string? modelName)
        => !string.IsNullOrWhiteSpace(modelName)
           && modelName.Contains("claude-sonnet-5", StringComparison.OrdinalIgnoreCase);
}
