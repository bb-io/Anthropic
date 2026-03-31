using Newtonsoft.Json;

namespace Apps.Anthropic.Constants;

public static class JsonOptions
{
    public static JsonSerializerSettings JsonSettings => new()
    {
        NullValueHandling = NullValueHandling.Ignore,
    };
}
