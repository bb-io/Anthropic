using Blackbird.Filters.Transformations;

namespace Apps.Anthropic.Utils;

public static class PromptBuilder
{
    public static string BuildTranslateUserPrompt(string? additionalInstructions, Transformation transformation, string json)
    {
        string baseInstruction = $"Translate each text from {transformation.SourceLanguage} to {transformation.TargetLanguage}.\n";

        if (!string.IsNullOrWhiteSpace(additionalInstructions))
        {
            baseInstruction += $"\n# Additional instructions\n{additionalInstructions}\n";
        }
        
        return
        $@"You are a professional translator specializing in localization. Your task is to process translation units from an XLIFF file.

        # **Instructions:**
        {baseInstruction}

        # **Critical Requirements:**
        - Translate each text individually, even if duplicates exist
        - Preserve ALL XML tags exactly as they appear (do not modify, remove, or relocate them)
        - Maintain the same text structure and formatting
        - Ensure translations are contextually appropriate and linguistically accurate

        **Output Format:**
        Return a valid JSON array where each object contains:
        - ""id"": the original ID (unchanged)
        - ""translation"": the translated text

        **Example Output:**
        [
          {{
            ""id"": ""item1"",
            ""translation"": ""translated text here""
          }},
          {{
            ""id"": ""item2"", 
            ""translation"": ""another translated text""
          }}
        ]

        **Input Data:**
        {json}

        Respond only with the JSON array, no additional text or formatting.";
    }

    public static string BuildTranslateSystemPrompt()
    {
        return @"You are an expert localization specialist with deep knowledge of cultural nuances, idiomatic expressions, and locale-specific references across multiple languages.

        Your core responsibilities:
        - Provide culturally appropriate translations that feel natural to native speakers
        - Maintain the exact structure and formatting of the original text
        - Preserve all XML tags, markup, and special characters exactly as they appear
        - Consider context and cultural sensitivity in your translations
        - Ensure consistency across similar translation units

        Always respond with valid JSON format only, without any additional commentary or explanation.";
    }
}
