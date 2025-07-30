using System.Globalization;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Anthropic.DataSourceHandlers.EnumHandlers;

public class LocaleDataSourceHandler : IDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData(DataSourceContext context)
    {
        return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Where(x => string.IsNullOrEmpty(context.SearchString) || x.DisplayName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(c => new DataSourceItem(c.Name, c.DisplayName));
    }
}