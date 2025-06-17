using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.RetryActions
{
    public class RetryActionWithAllTypesRequest
    {
        [Display("WebhookSite Url")]
        public string SendTo { get; set; }

        [Display("Throw exception number of times")]
        public int NumberOfRetries { get; set; }

        [Display("Text")]
        public string? InputText { get; set; }

        [Display("Retry action identifier", Description = "If you have more than one action in bird, specify some unique id here")]
        public string? RetryActionIdentifier { get; set; }
    }
}
