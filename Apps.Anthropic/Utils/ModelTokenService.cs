namespace Apps.Anthropic.Utils;

public static class ModelTokenService
{
    private static readonly Dictionary<string, int> ModelMaxTokens = new()
    {
        ["claude-opus-4-1-20250805"] = 32000,
        ["claude-opus-4-1"] = 32000,
        ["claude-opus-4-20250514"] = 32000,
        ["claude-opus-4-0"] = 32000,
        ["claude-sonnet-4-20250514"] = 64000,
        ["claude-sonnet-4-0"] = 64000,
        ["claude-3-7-sonnet-20250219"] = 64000,
        ["claude-3-7-sonnet-latest"] = 64000,
        ["claude-3-5-haiku-20241022"] = 8192,
        ["claude-3-5-haiku-latest"] = 8192,
        ["claude-3-haiku-20240307"] = 4096
    };

    public static int GetMaxTokensForModel(string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
        {
            return 4096;
        }

        return ModelMaxTokens.TryGetValue(modelName, out var maxTokens) ? maxTokens : 4096;
    }
}
