using Blackbird.Applications.SDK.Blueprints.Interfaces.Edit;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Anthropic.Models.Response;

public class EditTextResponse : IEditTextOutput
{
    [Display("Edited text")]
    public string EditedText { get; set; } = string.Empty;

    [Display("System prompt")]
    public string SystemPrompt { get; set; } = string.Empty;
    
    [Display("User prompt")]
    public string UserPrompt { get; set; } = string.Empty;

    public UsageResponse Usage { get; set; } = new();
}
