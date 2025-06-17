using Apps.Anthropic.DataSourceHandlers;
using Apps.Anthropic.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;
using TestPlugin.DynamicHandlers;
using TestPlugin.DynamicHandlers.StaticDataHandlers;

namespace Apps.Anthropic.Models.Request;

public class ProcessXliffRequest
{
    [Display("XLIFF file")]
    public FileReference Xliff { get; set; }

    [Display("Model", Description = "This parameter controls which version of Claude answers your request")]
    [DataSource(typeof(ModelDataSource))]
    [JsonProperty("model")]
    public string Model { get; set; }

    [Display("Prompt", Description = "The prompt that you want Claude to complete.")]
    [JsonProperty("prompt")]
    public string? Prompt { get; set; }

    [Display("System prompt", Description = "A system prompt is a way of providing context and instructions to Claude,\n such as specifying a particular goal or role for Claude before asking it a question or giving it a task.")]
    public string? SystemPrompt { get; set; }

    [Display("Max tokens", Description = "The maximum number of tokens to generate before stopping.")]
    [JsonProperty("max_tokens_to_sample")]
    public int? MaxTokensToSample { get; set; }

    [Display("Stop sequences", Description = "Sequences that will cause the model to stop generating completion text.")]
    [JsonProperty("stop_sequences")]
    public List<string>? StopSequences { get; set; }

    [Display("Temperature", Description = "Amount of randomness injected into the response.")]
    [DataSource(typeof(TemperatureDataSourceHandler))]
    [JsonProperty("temperature")]
    public string? Temperature { get; set; }

    [Display("top_p", Description = "Use nucleus sampling.\nIn nucleus sampling, we compute the cumulative distribution over all the options\n for each subsequent token in decreasing probability order and cut it off\n once it reaches a particular probability specified by top_p.\nYou should either alter temperature or top_p, but not both.")]
    [DataSource(typeof(TopPDataSourceHandler))]
    [JsonProperty("top_p")]
    public string? TopP { get; set; }

    [Display("top_k", Description = "Only sample from the top K options for each subsequent token.\nUsed to remove \"long tail\" low probability responses.")]
    [JsonProperty("top_k")]
    public int? TopK { get; set; }

    [DataSource(typeof(DynamicSimpleHandler))]
    [Display("Multiple Texts (Dynamic)", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 2.")]
    public List<string>? InputStringsDynamic { get; set; }

    [DataSource(typeof(DynamicItemsSimpleHandler))]
    [Display("Multiple Texts (Dynamic NH)", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 2.")]
    public List<string>? InputStringsDynamicNewHandler { get; set; }

    [StaticDataSource(typeof(SimpleStaticDataHandler))]
    [Display("Multiple Texts (Static)", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 2.")]
    public List<string>? InputStringsStatic { get; set; }

    [StaticDataSource(typeof(SimpleStaticItemsDataHandler))]
    [Display("Multiple Texts (Static NH)", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 2.")]
    public List<string>? InputStringsStaticNewHandler { get; set; }

    [Display("Multiple Texts", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 2.")]
    public List<string>? InputStrings { get; set; }

    [Display("Multiple Numbers", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 3.")]
    public List<int>? InputInts { get; set; }

    [Display("Multiple Booleans", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 5.")]
    public List<bool>? InputBooleans { get; set; }

    [Display("Multiple Dates", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 4.")]
    public List<DateTime>? InputDateTimes { get; set; }

    [Display("Multiple Files", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 6.")]
    public List<FileReference>? InputFiles { get; set; }
}