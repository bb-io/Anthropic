using Apps.Anthropic.Api.Interfaces;
using Apps.Anthropic.Extensions;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Utils;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;

namespace Apps.Anthropic.Ai;

public sealed class AnthropicLlmClient(IAnthropicClient client, string? model, AnthropicLlmSettings settings) : ILlmClient
{
    // Conservative for every model: buckets are bounded by the output limit long before this.
    private const int ContextTokens = 200_000;

    private static readonly HashSet<string> TruncatedStopReasons =
        new(StringComparer.OrdinalIgnoreCase) { "max_tokens", "model_context_window_exceeded" };

    private readonly object _gate = new();
    private UsageResponse _usage = new();

    public LlmCapabilities Capabilities { get; } = new(
        SupportsStructuredOutput: false,
        ContextTokens: ContextTokens,
        MaxOutputTokens: MaxOutputTokensFor(model, settings.MaxTokens));

    // LlmUsage carries only input/output tokens; this keeps the cache counters the action reports.
    public UsageResponse Usage
    {
        get
        {
            lock (_gate)
            {
                return _usage;
            }
        }
    }

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = await client.ExecuteChat(new MessageRequest
        {
            System = request.SystemPrompt,
            Model = model,
            Messages = [new Message { Role = "user", Content = request.UserPrompt }],
            MaxTokens = settings.MaxTokens ?? ModelCatalog.GetMaxOutputTokens(model),
            StopSequences = settings.StopSequences ?? [],
            Temperature = settings.Temperature.ToOptionalFloat("temperature"),
            TopP = settings.TopP.ToOptionalFloat("top_p"),
            TopK = settings.TopK,
            SkillId = settings.SkillId
        });

        lock (_gate)
        {
            _usage += response.Usage;
        }

        return new LlmResponse(
            response.Text,
            new LlmUsage(response.Usage.InputTokens, response.Usage.OutputTokens),
            response.StopReason is not null && TruncatedStopReasons.Contains(response.StopReason)
                ? FinishReason.Truncated
                : FinishReason.Stop);
    }

    private static int MaxOutputTokensFor(string? model, int? requested)
    {
        var limit = ModelCatalog.GetMaxOutputTokens(model);
        return requested is > 0 ? Math.Min(requested.Value, limit) : limit;
    }
}
