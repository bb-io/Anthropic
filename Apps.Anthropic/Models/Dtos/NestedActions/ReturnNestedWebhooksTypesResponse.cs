using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace TestPlugin.Dtos.NestedActions
{
    public class ReturnNestedWebhooksTypesResponse
    {
        [Display("Nested Object")]
        public NestedObject NestedObject { get; set; }

        [Display("Nested Objects In Array")]
        public IEnumerable<NestedObjectArrayItem> NestedObjects { get; set; }
    }
}
