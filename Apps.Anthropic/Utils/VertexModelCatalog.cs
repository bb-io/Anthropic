using Apps.Anthropic.Models.Response;

namespace Apps.Anthropic.Utils;

public static class VertexModelCatalog
{
    public static IReadOnlyList<ModelResponse> Models { get; } =
    [
        new("claude-fable-5", "Claude Fable 5"),
        new("claude-opus-4-8", "Claude Opus 4.8"),
        new("claude-opus-4-7", "Claude Opus 4.7"),
        new("claude-opus-4-6", "Claude Opus 4.6"),
        new("claude-sonnet-5", "Claude Sonnet 5"),
        new("claude-sonnet-4-6", "Claude Sonnet 4.6"),
        new("claude-sonnet-4-5@20250929", "Claude Sonnet 4.5"),
        new("claude-opus-4-5@20251101", "Claude Opus 4.5"),
        new("claude-haiku-4-5@20251001", "Claude Haiku 4.5")
    ];
}
