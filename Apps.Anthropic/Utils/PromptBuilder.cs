using Blackbird.Filters.Transformations;

namespace Apps.Anthropic.Utils;

public static class PromptBuilder
{
    private const string JsonOutputExample = @"```json
{
    ""translations"": [
        {
            ""translation_id"": ""123"",
            ""translated_text"": ""Translated text here""
        },
        {
            ""translation_id"": ""456"",
            ""translated_text"": ""Another translated text""
        }
    ]
}
```";
    
    public static string BuildTranslateUserPrompt(string? additionalInstructions, Transformation transformation,
        string json)
    {
        string baseInstruction =
            $"Translate each text from {transformation.SourceLanguage} to {transformation.TargetLanguage}.\n";

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
Return a valid JSON object that contains array where each object contains:
- ""translations"": an array of translation objects, each with:
- ""translation_id"": the original ID (unchanged)
- ""translated_text"": the translated text

**Example Output:**
{JsonOutputExample}

**Input Data:**
{json}

Respond only with the JSON array, no additional text or formatting.";
    }

    public static string BuildTranslateSystemPrompt()
    {
        return
            @"You are an expert localization specialist with deep knowledge of cultural nuances, idiomatic expressions, and locale-specific references across multiple languages.

Your core responsibilities:
- Provide culturally appropriate translations that feel natural to native speakers
- Maintain the exact structure and formatting of the original text
- Preserve all XML tags, markup, and special characters exactly as they appear
- Consider context and cultural sensitivity in your translations
- Ensure consistency across similar translation units

Always respond with valid JSON format only, without any additional commentary or explanation.";
    }

    public static string BuildEditUserPrompt(string? additionalInstructions, Transformation transformation,
        string json)
    {
        string baseInstruction =
            $"Edit and improve the translations from {transformation.SourceLanguage} to {transformation.TargetLanguage}.\n" +
            "Review each translation unit containing source text and current target translation. " +
            "Edit the target text to ensure accuracy, fluency, and natural expression in the target language.";

        if (!string.IsNullOrWhiteSpace(additionalInstructions))
        {
            baseInstruction += $"\n\n# Additional instructions\n{additionalInstructions}\n";
        }

        return
            $@"You are a professional translator specializing in post-editing XLIFF translations.

# **Instructions:**
{baseInstruction}

# **Critical Requirements:**
- Review each translation unit with its source text and current target translation
- Edit the target text only when improvements are needed for accuracy or fluency
- Preserve ALL XML tags exactly as they appear (do not modify, remove, or relocate them)
- Maintain the same text structure and formatting
- Ensure translations are contextually appropriate and linguistically accurate
- Focus on correcting errors and enhancing natural expression

**Output Format:**
Return a valid JSON object that contains array where each object contains:
- ""translations"": an array of translation objects, each with:
- ""translation_id"": the original ID (unchanged)
- ""translated_text"": the edited/improved translated text

**Example Output:**
{JsonOutputExample}

**Input Data (ID, Source Text, Current Target Text):**
{json}

Respond only with the JSON array, no additional text or formatting.";
    }

    public static string BuildEditSystemPrompt()
    {
        return
            @"You are a professional translator specializing in post-editing XLIFF translations with deep knowledge of cultural nuances, idiomatic expressions, and locale-specific references across multiple languages.

Your core responsibilities:
- Review and improve existing translations for accuracy and fluency
- Maintain culturally appropriate translations that feel natural to native speakers
- Preserve the exact structure and formatting of the original text
- Preserve all XML tags, markup, and special characters exactly as they appear
- Consider context and cultural sensitivity in your edits
- Ensure consistency across similar translation units
- Only edit when genuine improvements can be made

Always respond with valid JSON format only, without any additional commentary or explanation.";
    }
}