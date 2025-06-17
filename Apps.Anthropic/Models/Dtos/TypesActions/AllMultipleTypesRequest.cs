using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using TestPlugin.DynamicHandlers;
using TestPlugin.DynamicHandlers.StaticDataHandlers;

namespace TestPlugin.Dtos.TypesActions
{
    public class AllMultipleTypesRequest
    {
        [DataSource(typeof(DynamicSimpleHandler))]
        [Display("Multiple Texts (Dynamic)", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 1.")]
        public List<string>? InputStringsDynamic { get; set; }

        //[DataSource(typeof(DynamicItemsSimpleHandler))]
        //[Display("Input strings (dynamic new handler)", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 2.")]
        //public List<string>? InputStringsDynamicNewHandler { get; set; }

        [StaticDataSource(typeof(SimpleStaticDataHandler))]
        [Display("Multiple Texts (Static)", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 2.")]
        public List<string>? InputStringsStatic { get; set; }

        //[StaticDataSource(typeof(SimpleStaticItemsDataHandler))]
        //[Display("Input strings (static new handler)", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 2.")]
        //public List<string>? InputStringsStaticNewHandler { get; set; }

        [Display("Multiple Texts", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 3.")]
        public List<string>? InputStrings { get; set; }

        [Display("Multiple Numbers", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 4.")]
        public List<int>? InputInts { get; set; }

        [Display("Multiple Booleans", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 6.")]
        public List<bool>? InputBooleans { get; set; }

        [Display("Multiple Dates", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 5.")]
        public List<DateTime>? InputDateTimes { get; set; }

        //[Display("Input files", Description = "This is test description of the item for testing purposes. This text should look fine. Test tooltip 6.")]
        //public List<FileReference>? InputFiles { get; set; }
    }
}
