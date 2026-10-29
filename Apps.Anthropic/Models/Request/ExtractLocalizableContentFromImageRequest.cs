using Apps.Anthropic.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Request;

public class ExtractLocalizableContentFromImageRequest
{
    [Display("Image")]
    public FileReference Image { get; set; } = new();

    [Display("Source language", Description = "Optional source language. If omitted, the model detects it from the image.")]
    [DataSource(typeof(LocaleDataSourceHandler))]
    public string? SourceLanguage { get; set; }

    [Display("Output file name", Description = "The name of the output XLIFF file without the extension.")]
    public string? OutputFileName { get; set; }
}
