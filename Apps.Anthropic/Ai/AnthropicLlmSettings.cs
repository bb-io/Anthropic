namespace Apps.Anthropic.Ai;

public sealed record AnthropicLlmSettings(
    string? Temperature = null,
    string? TopP = null,
    int? TopK = null,
    int? MaxTokens = null,
    List<string>? StopSequences = null,
    string? SkillId = null);
