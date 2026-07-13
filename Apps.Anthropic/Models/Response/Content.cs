namespace Apps.Anthropic.Models.Response;

public class Content : ITextContentBlock
{
    public string Type { get; set; }
    public string Text { get; set; }
}