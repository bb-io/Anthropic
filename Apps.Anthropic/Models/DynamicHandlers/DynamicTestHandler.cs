using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using TestPlugin.Dtos.DynamicInputActions;

namespace TestPlugin.DynamicHandlers
{
    public class DynamicTestHandler : BaseInvocable, IDataSourceHandler
    {
        public DynamicForContextRequest Input { get; set; }

        public DynamicTestHandler(InvocationContext invocationContext, [ActionParameter] DynamicForContextRequest input) : base(invocationContext)
        {
            Input = input;
        }

        public Dictionary<string, string> GetData(DataSourceContext context)
        {
            Dictionary<string, string> dictionary = new() { 
                { "Text", string.IsNullOrEmpty(Input.String) ? "empty" : Input.String }, 
                { "Number", Input.Number != 0 ? Input.Number.ToString() : "empty"  }, 
                { "Boolean", Input.Boolean.ToString() },
                { "Date", Input.Date != DateTime.MinValue ? Input.Date.ToString() : "empty" },
                { "Multiple Texts", string.IsNullOrEmpty(string.Join(',', Input.PrimitiveArrayStrings ?? new List<string>())) ? "empty" : string.Join(',', Input.PrimitiveArrayStrings ?? new List<string>()) },
                { "Dynamic Input Another", string.IsNullOrEmpty(Input.DynamicInputAnother) ? "empty" : Input.DynamicInputAnother },
                { "Text Optional", Input.StringOptional ?? "empty" },
                { "Number Optional", Input.NumberOptional != null ? Input.NumberOptional.ToString() : "empty" },
                { "Boolean Optional", Input.BooleanOptional != null ? Input.BooleanOptional.ToString() : "empty" },
                { "Date Optional", Input.DateOptional != null ? Input.DateOptional.ToString() : "empty" },
                { "Multiple Texts Optional", Input.PrimitiveArrayOptional != null ? string.Join(',', Input.PrimitiveArrayOptional ?? new List<string>()) : "empty" }
            };
            return dictionary.Where(x => x.Value.Contains(context.SearchString)).ToDictionary(k => k.Key, v => v.Value);
        }
    }
}
