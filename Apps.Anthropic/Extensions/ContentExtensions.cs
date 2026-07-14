using Amazon.BedrockRuntime.Model;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Models.Response.Bedrock;

namespace Apps.Anthropic.Extensions;

public static class ContentExtensions
{
    public static string ExtractText(this IEnumerable<ITextContentBlock> content) =>
        string.Concat(content.Where(x => x.Type == "text").Select(x => x.Text));

    public static string ExtractText(this IEnumerable<ContentBlock> content) =>
        string.Concat(content.Where(x => x.Text != null).Select(x => x.Text));

    public static string ExtractText(this IEnumerable<ContentBedrockResponse> content) =>
        string.Concat(content.Where(x => x.Text != null).Select(x => x.Text));
}
