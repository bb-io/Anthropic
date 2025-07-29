namespace Apps.Anthropic.Models.Entities;

public record DeserializeTranslationEntitiesResult(List<TranslationEntity> Translations, bool Success, string Error);