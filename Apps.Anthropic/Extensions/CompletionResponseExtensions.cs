using Apps.Anthropic.Models.Response;
using Newtonsoft.Json.Linq;

namespace Apps.Anthropic.Extensions;

public static class CompletionResponseExtensions
{
    private const string TextBlockType = "text";
    private const string WebSearchResultBlockType = "web_search_tool_result";

    public static ResponseMessage ToResponseMessage(this IReadOnlyCollection<CompletionResponse> completions)
    {
        var content = completions.SelectMany(completion => completion.Content).ToList();

        return new ResponseMessage
        {
            Text = content.ExtractText(),
            Usage = completions.Aggregate(new UsageResponse(), (total, completion) => total + completion.Usage),
            StopReason = completions.Last().StopReason,
            Citations = ExtractCitations(content),
            Sources = ExtractSources(content)
        };
    }

    private static List<CitationResponse> ExtractCitations(IEnumerable<Content> content) =>
        content
            .Where(block => block.Type == TextBlockType)
            .SelectMany(block => block.Citations ?? [])
            .Where(citation => !string.IsNullOrWhiteSpace(citation.Url))
            .DistinctBy(citation => citation.Url)
            .Select(citation => new CitationResponse { Title = citation.Title, Url = citation.Url })
            .ToList();

    private static List<string> ExtractSources(IEnumerable<Content> content) =>
        content
            .Where(block => block.Type == WebSearchResultBlockType)
            .Select(block => block.ToolResultContent)
            .OfType<JArray>()
            .SelectMany(results => results.OfType<JObject>())
            .Select(result => result.Value<string>("url"))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url!)
            .Distinct()
            .ToList();
}
