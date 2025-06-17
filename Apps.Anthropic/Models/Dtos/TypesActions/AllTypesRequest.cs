using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using TestPlugin.DynamicHandlers.StaticDataHandlers;
using TestPlugin.DynamicHandlers;
using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.TypesActions
{
    public class AllTypesRequest
    {
        [DataSource(typeof(DynamicSimpleHandler))]
        [Display("Text (Dynamic)")]
        public string? StringDynamic { get; set; }

        //[DataSource(typeof(DynamicItemsSimpleHandler))]
        //[Display("SomeString (dynamic new handler)")]
        //public string? SomeStringDynamicNewHandler { get; set; }
        
        [StaticDataSource(typeof(SimpleStaticDataHandler))]
        [Display("Text (Static)")]
        public string? StringStatic { get; set; }

        //[StaticDataSource(typeof(SimpleStaticItemsDataHandler))]
        //[Display("SomeString (static new handler)")]
        //public string? SomeStringStaticNewHandler { get; set; }

        [Display("Text")]
        public string? String { get; set; }

        [Display("Number")]
        public int? Int { get; set; }

        [Display("Boolean")]
        public bool? Boolean { get; set; }

        [Display("Date")]
        public DateTime? Date { get; set; }
    }
}
