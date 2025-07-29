using System.Text.RegularExpressions;
using Apps.Anthropic.Models.Entities;
using Newtonsoft.Json;

namespace Apps.Anthropic.Utils;

public static class ResponseDeserializationHelper
{
    public static DeserializeTranslationEntitiesResult DeserializeResponse(string content)
    {
        try
        {
            var deserializedResponse = JsonConvert.DeserializeObject<TranslationEntities>(content);
            return new(deserializedResponse.Translations, true, string.Empty);
        }
        catch (Exception ex)
        {
            var partialTranslations = ExtractValidTranslationsFromIncompleteJsonWithErrorHandling(content);
            
            if (partialTranslations.Count > 0)
            {
                return new(partialTranslations, true, $"Partial deserialization succeeded with {partialTranslations.Count} translations. Original error: {ex.Message}");
            }
            
            var truncatedContent = content.Substring(0, Math.Min(content.Length, 200)) + "...";
            return new(new(), false, $"Failed to deserialize OpenAI response: {ex.Message}. Response: {truncatedContent}");
        }
    }
    
    public static DeserializeReviewEntitiesResult DeserializeReviewResponse(string content)
    {
        try
        {
            var deserializedResponse = JsonConvert.DeserializeObject<ReviewEntities>(content)!;
            return new(deserializedResponse.Translations, true, string.Empty);
        }
        catch (Exception ex)
        {
            var partialReviews = ExtractValidReviewsFromIncompleteJsonWithErrorHandling(content);
            
            if (partialReviews.Count > 0)
            {
                return new(partialReviews, true, $"Partial deserialization succeeded with {partialReviews.Count} reviews. Original error: {ex.Message}");
            }
            
            var truncatedContent = content.Substring(0, Math.Min(content.Length, 200)) + "...";
            return new(new(), false, $"Failed to deserialize review response: {ex.Message}. Response: {truncatedContent}");
        }
    }

    private static List<TranslationEntity> ExtractValidTranslationsFromIncompleteJsonWithErrorHandling(string incompleteJson)
    {
        try 
        {
            return ExtractValidTranslationsFromIncompleteJson(incompleteJson);
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static List<TranslationEntity> ExtractValidTranslationsFromIncompleteJson(string incompleteJson)
    {
        var result = new List<TranslationEntity>();
        var pattern = @"\{\s*""translation_id""\s*:\s*""([^""]+)""\s*,\s*""translated_text""\s*:\s*""((?:[^""\\]|\\.)*)""(?:\s*,\s*""quality_score""\s*:\s*([0-9.]+))?\s*\}";
        
        var matches = Regex.Matches(incompleteJson, pattern);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                var entity = new TranslationEntity
                {
                    TranslationId = match.Groups[1].Value,
                    TranslatedText = ProcessJsonString(match.Groups[2].Value)
                };
                
                if (match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value))
                {
                    if (float.TryParse(match.Groups[3].Value, out float score))
                    {
                        entity.QualityScore = score;
                    }
                }
                
                result.Add(entity);
            }
        }
        
        return result;
    }

    private static string ProcessJsonString(string jsonString)
    {
        return Regex.Replace(jsonString, @"\\(.)", match =>
        {
            char escapedChar = match.Groups[1].Value[0];
            switch (escapedChar)
            {
                case 'n': return "\n";
                case 'r': return "\r";
                case 't': return "\t";
                case '\\': return "\\";
                case '"': return "\"";
                default: return match.Value;
            }
        });
    }

    private static List<ReviewEntity> ExtractValidReviewsFromIncompleteJsonWithErrorHandling(string incompleteJson)
    {
        try 
        {
            return ExtractValidReviewsFromIncompleteJson(incompleteJson);
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static List<ReviewEntity> ExtractValidReviewsFromIncompleteJson(string incompleteJson)
    {
        var result = new List<ReviewEntity>();
        var pattern = @"\{\s*""translation_id""\s*:\s*""([^""]+)""\s*,\s*""quality_score""\s*:\s*([0-9.]+)\s*\}";
        
        var matches = Regex.Matches(incompleteJson, pattern);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3 && float.TryParse(match.Groups[2].Value, out float score))
            {
                var entity = new ReviewEntity
                {
                    TranslationId = match.Groups[1].Value,
                    QualityScore = score
                };
                
                result.Add(entity);
            }
        }
        
        return result;
    }
}