using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.DynamicInputActions
{
    public class DynamicResponse
    {
        [Display("Selected Value In Dynamic Input")]
        public string? SelectedValue1 { get; set; }

        [Display("Selected Value In Dynamic Input (NH)")]
        public string? SelectedValue2 { get; set; }
    }
}
