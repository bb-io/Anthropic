using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;

namespace Tests.Anthropic.Ai;

// Offline stand-in for the provider: answers every request with a fixed text and records what was sent.
public sealed class RecordingLlmClient(Func<LlmRequest, string> answer) : ILlmClient
{
    public List<LlmRequest> Requests { get; } = [];

    public LlmCapabilities Capabilities { get; } = new(SupportsStructuredOutput: false, ContextTokens: 200_000, MaxOutputTokens: 8_192);

    public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        lock (Requests)
        {
            Requests.Add(request);
        }

        return Task.FromResult(LlmResponse.Completed(answer(request), new LlmUsage(10, 2)));
    }
}
