using Apps.Anthropic.DataSourceHandlers;
using Apps.Anthropic.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Request;

public class CompletionRequest
{
    [Display("Prompt", Description = "The prompt that you want Claude to complete.")]
    public required string Prompt { get; set; }

    [Display("System prompt", Description = "A system prompt is a way of providing context and instructions to Claude,\n such as specifying a particular goal or role for Claude before asking it a question or giving it a task.")]
    public string? SystemPrompt { get; set; }

    [Display("Max tokens", Description = "The maximum number of tokens to generate before stopping.")]
    public int? MaxTokensToSample { get; set; }

    [Display("Stop sequences", Description = "Sequences that will cause the model to stop generating completion text.")]
    public List<string>? StopSequences { get; set; }

    [Display("Temperature", Description = "Amount of randomness injected into the response.")]
    [DataSource(typeof(TemperatureDataSourceHandler))]
    public string? Temperature { get; set; }

    [Display("top_p", Description = "Use nucleus sampling.\nIn nucleus sampling, we compute the cumulative distribution over all the options\n for each subsequent token in decreasing probability order and cut it off\n once it reaches a particular probability specified by top_p.\nYou should either alter temperature or top_p, but not both.")]
    [DataSource(typeof(TopPDataSourceHandler))]
    public string? TopP { get; set; }

    [Display("top_k", Description = "Only sample from the top K options for each subsequent token.\nUsed to remove \"long tail\" low probability responses.")]
    public int? TopK { get; set; }

    [Display("File")]
    public FileReference? File { get; set; }
    
    [Display("Skill ID"), DataSource(typeof(SkillDataSource))]
    public string? SkillId { get; set; }

    [Display("Enable web search", Description = "Lets Claude search the web for up-to-date information and cite its sources. Not available for Amazon Bedrock connections.")]
    public bool? EnableWebSearch { get; set; }

    [Display("Max web searches", Description = "The maximum number of web searches Claude can run for this request.")]
    public int? MaxWebSearches { get; set; }

    [Display("Allowed domains", Description = "Only include web search results from these domains, e.g. example.com. Cannot be combined with blocked domains.")]
    public IEnumerable<string>? AllowedDomains { get; set; }

    [Display("Blocked domains", Description = "Never include web search results from these domains, e.g. example.com. Cannot be combined with allowed domains.")]
    public IEnumerable<string>? BlockedDomains { get; set; }

    [Display("User location city", Description = "Localizes web search results.")]
    public string? UserLocationCity { get; set; }

    [Display("User location region", Description = "Localizes web search results.")]
    public string? UserLocationRegion { get; set; }

    [Display("User location country", Description = "Two-letter ISO country code, e.g. US. Localizes web search results.")]
    public string? UserLocationCountry { get; set; }

    [Display("User location timezone", Description = "IANA timezone, e.g. Europe/Warsaw. Localizes web search results.")]
    public string? UserLocationTimezone { get; set; }
}