using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Entities;

public record DeserializeReviewEntitiesResult(List<ReviewEntity> Reviews, bool IsSuccess, string ErrorMessage);

public class ReviewEntities
{
    [JsonProperty("translations")]
    public List<ReviewEntity> Translations { get; set; } = new();
}

public class ReviewEntity
{
    [JsonProperty("translation_id")]
    public string TranslationId { get; set; } = string.Empty;

    [JsonProperty("quality_score")]
    public float QualityScore { get; set; }
    
    [JsonProperty("comments")]
    public string? Comments { get; set; } = string.Empty;
}