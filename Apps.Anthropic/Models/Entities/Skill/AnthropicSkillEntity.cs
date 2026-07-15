using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Entities.Skill;

public class AnthropicSkillEntity
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("display_title")]
    public string DisplayTitle { get; set; } = string.Empty;
}