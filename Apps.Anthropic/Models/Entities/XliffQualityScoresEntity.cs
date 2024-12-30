using Apps.Anthropic.Models.Response;

namespace Apps.Anthropic.Models.Entities;

public record XliffQualityScoresEntity(double Score, UsageResponse Usage);