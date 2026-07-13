namespace Apps.Anthropic.Models.Response;

public interface ITextContentBlock
{
    string Type { get; }

    string Text { get; }
}
