using Apps.Anthropic.Constants;
using Apps.Anthropic.DataSourceHandlers;
using Apps.Anthropic.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.SDK.Blueprints.Handlers;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Request;

public class TranslateRequest : ITranslateFileInput
{
    public FileReference File { get; set; } = new();
    
    [Display("Source language")]
    [StaticDataSource(typeof(LocaleDataSourceHandler))]
    public string? SourceLanguage { get; set; }
    
    [Display("Target language")]
    [StaticDataSource(typeof(LocaleDataSourceHandler))]
    public string TargetLanguage { get; set; } = string.Empty;
    
    [Display("Output file handling", Description = "Determine the format of the output file. The default Blackbird behavior is to convert to XLIFF for future steps."), StaticDataSource(typeof(ProcessFileFormatHandler))]
    public string? OutputFileHandling { get; set; }

    [Display("Model", Description = "This parameter controls which version of Claude answers your request")]
    [DataSource(typeof(ModelDataSource))]
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [Display("Additional instructions", Description = "The additional instructions that you want to apply to the translation.\nFor example, 'Cater to an older audience.'")]
    [JsonProperty("prompt")]
    public string? AdditionalInstructions { get; set; }

    [Display("System prompt", Description = "A system prompt is a way of providing context and instructions to Claude,\n such as specifying a particular goal or role for Claude before asking it a question or giving it a task.")]
    public string? SystemPrompt { get; set; }

    [Display("Max tokens", Description = "The maximum number of tokens to generate before stopping.")]
    [JsonProperty("max_tokens_to_sample")]
    public int? MaxTokensToSample { get; set; }

    [Display("Stop sequences", Description = "Sequences that will cause the model to stop generating completion text.")]
    [JsonProperty("stop_sequences")]
    public List<string>? StopSequences { get; set; }

    [Display("Temperature", Description = "Amount of randomness injected into the response.")]
    [StaticDataSource(typeof(TemperatureDataSourceHandler))]
    [JsonProperty("temperature")]
    public string? Temperature { get; set; }

    [Display("top_p", Description = "Use nucleus sampling.\nIn nucleus sampling, we compute the cumulative distribution over all the options\n for each subsequent token in decreasing probability order and cut it off\n once it reaches a particular probability specified by top_p.\nYou should either alter temperature or top_p, but not both.")]
    [StaticDataSource(typeof(TopPDataSourceHandler))]
    [JsonProperty("top_p")]
    public string? TopP { get; set; }

    [Display("top_k", Description = "Only sample from the top K options for each subsequent token.\nUsed to remove \"long tail\" low probability responses.")]
    [JsonProperty("top_k")]
    public int? TopK { get; set; }
    
    public FileReference? Glossary { get; set; }
    
    [Display("Bucket size", Description = "Specify the number of source texts to be translated at once. Default value: 50. (See our documentation for an explanation)")]
    public int? BucketSize { get; set; } = XliffConstants.DefaultBucketSize;

    [Display("Ignore deserialization errors", Description = "If enabled, the action will ignore deserialization errors and return the response as is.")]
    public bool? IgnoreDeserializationErrors { get; set; }
}