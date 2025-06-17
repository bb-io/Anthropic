using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using TestPlugin.DynamicHandlers;
using TestPlugin.DynamicHandlers.StaticDataHandlers;

namespace TestPlugin.Dtos.RetryActions
{
    public class RetryActionWithInputSingleItemsRequest
    {
        [Display("WebhookSite Url")]
        public string SendTo { get; set; }

        [Display("Throw exception number of times")]
        public int NumberOfRetries { get; set; }

        [Display("Retry action identifier", Description = "If you have more than one action in bird, specify some unique id here")]
        public string? RetryActionIdentifier { get; set; }

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
}
