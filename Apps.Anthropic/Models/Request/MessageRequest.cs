using Apps.Anthropic.Models.Dto;

namespace Apps.Anthropic.Models.Request;

public class MessageRequest
{
    public string System { get; set; }

    public string Model { get; set; }

    public List<Message> Messages { get; set; } = [];

    public int? MaxTokens { get; set; }

    public List<string> StopSequences { get; set; }

    public float? Temperature { get; set; }

    public float? TopP { get; set; }

    public int? TopK { get; set; }

    public InputFileData? FileData { get; set; }
}
