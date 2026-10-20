using Apps.Anthropic.DataSourceHandlers;
using Blackbird.Applications.SDK.Blueprints.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Anthropic.Models.Request;

public class BackgroundDownloadRequest
{
    [Display("Transformation file", Description = "The transformation file returned by 'Translate in background', 'Edit in background' or 'Review in background'.")]
    public FileReference TransformationFile { get; set; } = new();

    [Display("Batch ID", Description = "Optional. The transformation file carries the batch it was started with; when given, it must match."), DataSource(typeof(BatchDataSource))]
    public string? BatchId { get; set; }

    [Display("Output file handling", Description = "Determine the format of the output file. Default is the format chosen when the background job was started, otherwise XLIFF."), StaticDataSource(typeof(ProcessFileFormatHandler))]
    public string? OutputFileHandling { get; set; }
}
