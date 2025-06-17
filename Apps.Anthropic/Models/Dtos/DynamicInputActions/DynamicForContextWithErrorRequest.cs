using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using TestPlugin.DynamicHandlers;

namespace TestPlugin.Dtos.DynamicInputActions
{
    public class DynamicForContextWithErrorRequest
    {
        [Display("Dynamic Input"), DataSource(typeof(DynamicTestErrorHandler))]
        public string DynamicInput { get; set; }

        [Display("Dynamic Input (NH)"), DataSource(typeof(DynamicItemsTestErrorHandler))]
        public string DynamicInputNewHandler { get; set; }

        [Display("Dynamic Array Input"), DataSource(typeof(DynamicTestErrorHandler))]
        public List<string> DynamicArrayInput { get; set; }

        [Display("Text")]
        public string String { get; set; }

        [Display("Number")]
        public double Number { get; set; }

        [Display("Boolean")]
        public bool Boolean { get; set; }

        [Display("Date")]
        public DateTime Date { get; set; }

        [Display("Multiple Texts")]
        public IEnumerable<string> PrimitiveArrayStrings { get; set; }

        [Display("Dynamic Input Another"), DataSource(typeof(DynamicTestHandler))]
        public string DynamicInputAnother { get; set; }
        
        [Display("Dynamic Input Another (NH)"), DataSource(typeof(DataSourceItemsTestHandler))]
        public string DynamicInputAnotherNewHandler { get; set; }

        [Display("Text Optional")]
        public string? StringOptional { get; set; }

        [Display("Number Optional")]
        public double? NumberOptional { get; set; }

        [Display("Boolean Optional")]
        public bool? BooleanOptional { get; set; }

        [Display("Date Optional")]
        public DateTime? DateOptional { get; set; }

        [Display("Multiple Texts Optional")]
        public IEnumerable<string>? PrimitiveArrayOptional { get; set; }
    }
}
