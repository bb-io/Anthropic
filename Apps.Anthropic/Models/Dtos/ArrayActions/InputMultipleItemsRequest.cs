using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using TestPlugin.DynamicHandlers;
using TestPlugin.DynamicHandlers.StaticDataHandlers;

namespace TestPlugin.Dtos.ArrayActions
{
    public class InputMultipleItemsRequest
    {
        [DataSource(typeof(DynamicSimpleHandler))]
        [Display("Input strings (dynamic)",
            Description =
                "This is test description of the item for testing purposes. This text should look fine. Test tooltip 2.")]
        public List<string>? InputStringsDynamic { get; set; }

        [StaticDataSource(typeof(SimpleStaticDataHandler))]
        [Display("Input strings (static)",
            Description =
                "This is test description of the item for testing purposes. This text should look fine. Test tooltip 2.")]
        public List<string>? InputStringsStatic { get; set; }

        [Display("Input strings",
            Description =
                "This is test description of the item for testing purposes. This text should look fine. Test tooltip 2.")]
        public List<string>? InputStrings { get; set; }

        [Display("Input ints",
            Description =
                "This is test description of the item for testing purposes. This text should look fine. Test tooltip 3.")]
        public List<int>? InputInts { get; set; }

        [Display("Input date times",
            Description =
                "This is test description of the item for testing purposes. This text should look fine. Test tooltip 4.")]
        public List<DateTime>? InputDateTimes { get; set; }

        [Display("Input booleans",
            Description =
                "This is test description of the item for testing purposes. This text should look fine. Test tooltip 5.")]
        public List<bool>? InputBooleans { get; set; }

        [Display("Input files",
            Description =
                "This is test description of the item for testing purposes. This text should look fine. Test tooltip 6.")]
        public List<FileReference>? InputFiles { get; set; }
    }
}