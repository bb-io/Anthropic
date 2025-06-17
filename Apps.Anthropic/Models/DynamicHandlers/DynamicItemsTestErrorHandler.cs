using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using TestPlugin.Dtos.DynamicInputActions;

namespace TestPlugin.DynamicHandlers;

public class DynamicItemsTestErrorHandler : BaseInvocable, IDataSourceItemHandler
{
    public DynamicForContextRequest Input { get; set; }

    public DynamicItemsTestErrorHandler(InvocationContext invocationContext,
        [ActionParameter] DynamicForContextRequest input) : base(invocationContext)
    {
        Input = input;
    }

    public IEnumerable<DataSourceItem> GetData(DataSourceContext context)
    {
        Dictionary<string, string> dictionary = new()
        {
            {
                "Text",
                !string.IsNullOrEmpty(Input.String)
                    ? Input.String
                    : throw new ArgumentException("Fill in Text value")
            },
            {
                "Number",
                Input.Number != 0.0
                    ? Input.Number.ToString()
                    : throw new ArgumentException("Fill in Number value")
            },
            { "Boolean", Input.Boolean.ToString() },
            {
                "Date",
                Input.Date != DateTime.MinValue
                    ? Input.Date.ToString()
                    : throw new ArgumentException("Fill in Date value")
            },
            {
                "Multiple Texts",
                Input.PrimitiveArrayStrings != null && Input.PrimitiveArrayStrings.Count() != 0
                    ? string.Join(',', Input.PrimitiveArrayStrings ?? new List<string>())
                    : throw new ArgumentException("Fill at least one value in Multiple Texts")
            },
            {
                "Dynamic Input Another (NH)",
                !string.IsNullOrEmpty(Input.DynamicInputAnotherNewHandler)
                    ? Input.DynamicInputAnotherNewHandler
                    : throw new ArgumentException("Fill in Dynamic Input Another (NH) value")
            },
            { "Text Optional", Input.StringOptional ?? "" },
            { "Number Optional", Input.NumberOptional != null ? Input.NumberOptional.ToString() : "" },
            { "Boolean Optional", Input.BooleanOptional != null ? Input.BooleanOptional.ToString() : "" },
            { "Date Optional", Input.DateOptional != null ? Input.DateOptional.ToString() : "" },
            {
                "Multiple Texts Optional",
                Input.PrimitiveArrayOptional != null
                    ? string.Join(',', Input.PrimitiveArrayStrings ?? new List<string>())
                    : ""
            }
        };
        return dictionary.Where(x => x.Value.Contains(context.SearchString)).ToDictionary(k => k.Key, v => v.Value)
            .Select(x => new DataSourceItem(x.Key, x.Value));
    }
}