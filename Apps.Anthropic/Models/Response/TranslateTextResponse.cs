using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Anthropic.Models.Response;

public class TranslateTextResponse : ITranslateTextOutput
{
    [Display("Translated text")]
    public string TranslatedText { get; set; } = string.Empty;

    [Display("System prompt")]
    public string SystemPrompt { get; set; } = string.Empty;
    
    [Display("User prompt")]
    public string UserPrompt { get; set; } = string.Empty;

    public UsageResponse Usage { get; set; } = new();
}
