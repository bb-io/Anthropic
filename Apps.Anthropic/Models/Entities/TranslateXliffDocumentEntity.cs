using Apps.Anthropic.Models.Response;

namespace Apps.Anthropic.Models.Entities;

public record TranslateXliffDocumentEntity(Dictionary<string,string> TranslationUnits, UsageResponse Usage);