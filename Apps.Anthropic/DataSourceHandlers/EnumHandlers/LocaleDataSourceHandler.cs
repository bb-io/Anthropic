using System.Globalization;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Anthropic.DataSourceHandlers.EnumHandlers;

public class LocaleDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return CultureInfo.GetCultures(CultureTypes.SpecificCultures).Select(c => new DataSourceItem(c.Name, c.DisplayName));
    }
}