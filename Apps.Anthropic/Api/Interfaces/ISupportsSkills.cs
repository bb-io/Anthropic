using Apps.Anthropic.Models.Dto;

namespace Apps.Anthropic.Api.Interfaces;

public interface ISupportsSkills
{
    Task<List<SkillDto>> ListSkills();
}