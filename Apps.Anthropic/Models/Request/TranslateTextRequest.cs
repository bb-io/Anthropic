using Apps.Anthropic.DataSourceHandlers;
using Apps.Anthropic.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Request;

public class TranslateTextRequest : ITranslateTextInput
{
    [Display("Text to translate")]
    public string Text { get; set; } = string.Empty;
    
    [Display("Source language")]
    [DataSource(typeof(LocaleDataSourceHandler))]
    public string? SourceLanguage { get; set; }
    
    [Display("Target language")]
    [DataSource(typeof(LocaleDataSourceHandler))]
    public string TargetLanguage { get; set; } = string.Empty;

    [Display("Model", Description = "This parameter controls which version of Claude answers your request")]
    [DataSource(typeof(ModelDataSource))]
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [Display("Additional instructions", Description = "The additional instructions that you want to apply to the translation.\nFor example, 'Cater to an older audience.'")]
    public string? AdditionalInstructions { get; set; }

    [Display("Max tokens", Description = "The maximum number of tokens to generate before stopping.")]
    [JsonProperty("max_tokens_to_sample")]
    public int? MaxTokensToSample { get; set; }

    [Display("Temperature", Description = "Amount of randomness injected into the response.")]
    [DataSource(typeof(TemperatureDataSourceHandler))]
    [JsonProperty("temperature")]
    public string? Temperature { get; set; }

    [Display("top_p", Description = "Use nucleus sampling.")]
    [DataSource(typeof(TopPDataSourceHandler))]
    [JsonProperty("top_p")]
    public string? TopP { get; set; }

    [Display("top_k", Description = "Only sample from the top K options for each subsequent token.")]
    [JsonProperty("top_k")]
    public int? TopK { get; set; }

    public FileReference? Glossary { get; set; }
}
