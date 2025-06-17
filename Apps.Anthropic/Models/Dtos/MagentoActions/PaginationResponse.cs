using Blackbird.Applications.Sdk.Common;

namespace TestPlugin.Dtos.MagentoActions;

public class PaginationResponse<T>
{
    public virtual List<T> Items { get; set; } = new();
    
    [DefinitionIgnore]
    public SearchCriteriaDto SearchCriteria { get; set; } = new();
    
    [Display("Total count")]
    public int TotalCount { get; set; }
}

public class SearchCriteriaDto
{
    [DefinitionIgnore]
    public List<FilterGroup> FilterGroups { get; set; } = new();
}

public class FilterGroup
{
}