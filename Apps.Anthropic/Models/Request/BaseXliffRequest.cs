using Apps.Anthropic.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;

namespace Apps.Anthropic.Models.Request;

public class BaseXliffRequest
{
    public FileReference File { get; set; } = default!;

    [Display("Model", Description = "This parameter controls which version of Claude answers your request")]
    [StaticDataSource(typeof(ModelDataSourceHandler))]
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [Display("Max tokens")]
    public int? MaxTokens { get; set; }
    
    public FileReference? Glossary { get; set; }
}