namespace Apps.Anthropic.Models.Response;

public class ResponseMessage
{
    public string Text { get; set; } = string.Empty;

    public UsageResponse Usage { get; set; } = new();
}