using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestPlugin.Utils;

public class StringOrUndefinedConverter : JsonConverter<string>
{
    public override string ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        try
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return "undefined";
            }

            if (reader.TokenType == JsonToken.StartArray)
            {
                var items = JArray.Load(reader);
                var stringItems = new List<string>();
                foreach (var item in items)
                {
                    stringItems.Add(item.ToString());
                }
                return $"[{string.Join(", ", stringItems)}]";
            }

            return reader.Value?.ToString() ?? "undefined";
        }
        catch
        {
            return "undefined";
        }
    }

    public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }
}