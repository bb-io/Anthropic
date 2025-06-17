using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;
using TestPlugin.DynamicHandlers;
using TestPlugin.DynamicHandlers.StaticDataHandlers;

namespace Apps.Anthropic.Models.Response;

public class BatchResponse
{
    [Display("Batch ID")]
    public string Id { get; set; } = string.Empty;

    [Display("Batch type")]
    public string Type { get; set; } = string.Empty;
    
    [Display("Processing status"), JsonProperty("processing_status")]
    public string ProcessingStatus { get; set; } = string.Empty;

    [Display("Request counts"), JsonProperty("request_counts")]
    public RequestCountResponse RequestCounts { get; set; } = new();

    [DataSource(typeof(DynamicSimpleHandler))]
    [Display("Text (Dynamic)")]
    public string? InputTextDynamic { get; set; }

    [DataSource(typeof(DynamicItemsSimpleHandler))]
    [Display("Text (Dynamic NH)")]
    public string? InputTextDynamicNewHandler { get; set; }

    [StaticDataSource(typeof(SimpleStaticDataHandler))]
    [Display("Text (Static)")]
    public string? InputTextStatic { get; set; }

    [StaticDataSource(typeof(SimpleStaticItemsDataHandler))]
    [Display("Text (Static NH)")]
    public string? InputTextStaticNewHandler { get; set; }

    [Display("Text")]
    public string? InputText { get; set; }

    [Display("Number")]
    public double? InputNumber { get; set; }

    [Display("Boolean")]
    public bool? InputBoolean { get; set; }

    [Display("Date")]
    public DateTime? InputDate { get; set; }

    [Display("File")]
    public FileReference? InputFile { get; set; }
}