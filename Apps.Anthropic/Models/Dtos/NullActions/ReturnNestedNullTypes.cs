using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.NullActions
{
    public class ReturnNestedNullTypes
    {
        [Display("Nested Object With Nulls")]
        public ReturnNullTypesResponse NestedObjectWithNulls { get; set; }
    }
}
