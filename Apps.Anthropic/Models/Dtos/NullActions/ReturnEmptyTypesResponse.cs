using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.NullActions
{
    public class ReturnEmptyTypesResponse
    {
        [Display("Empty String")]
        public string EmptyString { get; set; }

        [Display("Empty Array")]
        public List<string> EmptyArray { get; set; }
    }
}
