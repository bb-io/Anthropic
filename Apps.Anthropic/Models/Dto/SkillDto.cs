namespace Apps.Anthropic.Models.Dto;

public class SkillDto(string id, string name)
{
    public string Id { get; set; } = id;

    public string Name { get; set; } = name;
}