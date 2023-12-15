using Apps.Anthropic.DataSourceHandlers;
using Apps.Anthropic.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Anthropic.Models.Request
{
    public class CompletionRequest
    {
        [Display("Model", Description = "This parameter controls which version of Claude answers your request")]
        [DataSource(typeof(ModelDataSourceHandler))]
        [JsonProperty("model")]
        public string Model { get; set; }

        [Display("Prompt", Description = "The prompt that you want Claude to complete.")]
        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [Display("System prompt", Description = "A system prompt is a way of providing context and instructions to Claude,\n such as specifying a particular goal or role for Claude before asking it a question or giving it a task.")]
        public string? SystemPrompt { get; set; }

        [Display("Max tokens to sample", Description = "The maximum number of tokens to generate before stopping.")]
        [JsonProperty("max_tokens_to_sample")]
        public int MaxTokensToSample { get; set; }

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
    }
}
