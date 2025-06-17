using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;
using TestPlugin.Utils;

namespace TestPlugin.Dtos.MagentoActions;

public class ProductsResponse : PaginationResponse<ProductResponse>
{
    [Display("Products")]
    public override List<ProductResponse> Items { get; set; } = new();
}

public class ProductResponse
{
    [Display("Product ID")]
    public string Id { get; set; } = string.Empty;
    
    [Display("SKU")]
    public string Sku { get; set; } = string.Empty;
    
    [Display("Product name")]
    public string Name { get; set; } = string.Empty;
    
    [Display("Attribute set ID")]
    public string AttributeSetId { get; set; } = string.Empty;
    
    [Display("Price")]
    public double Price { get; set; }
    
    [Display("Status")]
    public int Status { get; set; }
    
    [Display("Visibility")]
    public int Visibility { get; set; }
    
    [Display("Type ID")]
    public string TypeId { get; set; } = string.Empty;
    
    [Display("Created at"), JsonConverter(typeof(CustomDateTimeConverter))]
    public DateTime CreatedAt { get; set; }
    
    [Display("Updated at"), JsonConverter(typeof(CustomDateTimeConverter))]
    public DateTime UpdatedAt { get; set; }
    
    [Display("Weight")]
    public double Weight { get; set; }
    
    [DefinitionIgnore]
    public ExtensionAttributes ExtensionAttributes { get; set; } = new();
    
    [DefinitionIgnore]
    public List<ProductLink> ProductLinks { get; set; } = new();
    
    [DefinitionIgnore]
    public List<object> Options { get; set; } = new();
    
    [DefinitionIgnore]
    public List<object> MediaGalleryEntries { get; set; } = new();
    
    [DefinitionIgnore]
    public List<object> TierPrices { get; set; } = new();
    
    [Display("Custom attributes")]
    public List<CustomAttribute> CustomAttributes { get; set; } = new();
}

public class ExtensionAttributes
{
    [DefinitionIgnore]
    public List<int> WebsiteIds { get; set; } = new();
    
    [Display("Category links")]
    public List<CategoryLink> CategoryLinks { get; set; } = new();
    
    [DefinitionIgnore]
    public List<BundleProductOption> BundleProductOptions { get; set; } = new();
}

public class ProductLink
{
    [Display("SKU")]
    public string Sku { get; set; } = string.Empty;
    
    [Display("Link type")]
    public string LinkType { get; set; } = string.Empty;
    
    [Display("Linked product SKU")] 
    public string LinkedProductSku { get; set; } = string.Empty;
    
    [Display("Linked product type")]
    public string LinkedProductType { get; set; } = string.Empty;
    
    [Display("Position")]
    public int Position { get; set; }
}

public class Option
{
    [Display("Product SKU")] 
    public string ProductSku { get; set; } = string.Empty;
    
    [Display("Option ID")]
    public int OptionId { get; set; }
    
    [Display("Title")]
    public string Title { get; set; } = string.Empty;
    
    [Display("Type")]
    public string Type { get; set; } = string.Empty;
    
    [Display("Sort order")]
    public int SortOrder { get; set; }
    
    [Display("Is require")]
    public bool IsRequire { get; set; }
    
    [Display("SKU")]
    public string Sku { get; set; } = string.Empty;
    
    [Display("Max characters")]
    public int MaxCharacters { get; set; } 
    
    [Display("Image size X")]
    public int ImageSizeX { get; set; }
    
    [Display("Image size Y")]
    public int ImageSizeY { get; set; }
    
    [DefinitionIgnore]
    public List<Value> Values { get; set; } = new();
}

public class Value
{
    [Display("Title")]
    public string Title { get; set; } = string.Empty;
    
    [Display("Sort order")]
    public int SortOrder { get; set; }
    
    [Display("Price")]
    public decimal Price { get; set; }
    
    [Display("Price type")]
    public string PriceType { get; set; } = string.Empty;
     
    [Display("SKU")]
    public string Sku { get; set; } = string.Empty;
    
    [Display("Option type ID")]
    public int OptionTypeId { get; set; }
}

public class MediaGalleryEntry
{
    [Display("Media type")]
    public string MediaType { get; set; } = string.Empty;
    
    [Display("Label")]
    public string Label { get; set; } = string.Empty;
    
    [Display("Position")]
    public int Position { get; set; }
    
    [Display("Disabled")]
    public bool Disabled { get; set; }
    
    [Display("Types")]
    public List<string> Types { get; set; } = new();
    
    [Display("File")]
    public string File { get; set; } = string.Empty;
    
    [Display("Content")]
    public Content Content { get; set; } = new();
}

public class Content
{
    [Display("Base64 encoded data")]
    public string Base64EncodedData { get; set; } = string.Empty;
    
    [Display("Type")]
    public string Type { get; set; } = string.Empty;
    
    [Display("Name")]
    public string Name { get; set; } = string.Empty;
}

public class CustomAttribute
{
    [Display("Attribute code"), JsonProperty("attribute_code")]
    public string AttributeCode { get; set; } = string.Empty;
     
    [Display("Value"), JsonProperty("value"), JsonConverter(typeof(StringOrUndefinedConverter))]
    public string Value { get; set; } = string.Empty;
}

public class CategoryLink
{
    [Display("Position")]
    public int Position { get; set; }
    
    [Display("Category ID")]
    public string CategoryId { get; set; } = string.Empty;
}

public class BundleProductOption
{
    [Display("Option ID")]
    public int OptionId { get; set; }
    
    [Display("Title")]
    public string Title { get; set; } = string.Empty;
    
    [Display("Required")]
    public bool Required { get; set; }
    
    [Display("Type")]
    public string Type { get; set; } = string.Empty;
    
    [Display("Position")]
    public int Position { get; set; } 
    
    [Display("SKU")]
    public string Sku { get; set; } = string.Empty;
    
    [DefinitionIgnore]
    public List<ProductLink> ProductLinks { get; set; } = new();
}