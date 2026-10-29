namespace Apps.Anthropic.Utils;

public static class FileFormatHelper
{
    public static bool IsImage(string ext)
    {
        var normalizedExtension = ext.TrimStart('.').ToLowerInvariant();
        return new[] { "jpg", "jpeg", "png", "gif", "webp" }.Contains(normalizedExtension);
    }

    public static bool IsPdf(string ext)
    {
        return ext.EndsWith("pdf");
    }

    public static bool IsSupportedNatively(string ext)
    {
        return IsImage(ext) || IsPdf(ext);
    }

    public static string GetAnthropicImageMediaType(string ext) => ext.TrimStart('.').ToLowerInvariant() switch
    {
        "png" => "image/png",
        "gif" => "image/gif",
        "webp" => "image/webp",
        "jpeg" => "image/jpeg",
        "jpg" => "image/jpeg",
        _ => "image/jpeg"
    };
}
