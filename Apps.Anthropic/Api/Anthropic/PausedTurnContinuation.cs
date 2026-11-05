using Apps.Anthropic.Extensions;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;

namespace Apps.Anthropic.Api.Anthropic;

internal static class PausedTurnContinuation
{
    internal const int MaxContinuations = 5;
    private const string PauseTurnStopReason = "pause_turn";

    internal static async Task<ResponseMessage> ExecuteAsync(
        MessageRequest message,
        List<object> conversation,
        Func<Task<string>> sendRequestAsync)
    {
        var completions = new List<CompletionResponse>();

        while (true)
        {
            var responseJson = JObject.Parse(await sendRequestAsync());
            var completion = responseJson.ToObject<CompletionResponse>()
                             ?? throw new PluginApplicationException("Claude returned an empty response");
            completions.Add(completion);

            if (!ShouldContinue(message, completion, completions.Count))
            {
                return completions.ToResponseMessage();
            }

            conversation.Add(new { role = "assistant", content = responseJson["content"] });
        }
    }

    private static bool ShouldContinue(MessageRequest message, CompletionResponse completion, int responseCount) =>
        completion.StopReason == PauseTurnStopReason
        && message.WebSearchTool != null
        && string.IsNullOrEmpty(message.SkillId)
        && responseCount <= MaxContinuations;
}
