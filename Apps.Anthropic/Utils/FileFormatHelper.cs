namespace Apps.Anthropic.Utils;

public static class FileFormatHelper
{
    public static bool IsImage(string ext)
    {
        return new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext);
    }

    public static string GetAnthropicImageMediaType(string ext) => ext switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".jpeg" => "image/jpeg",
        ".jpg" => "image/jpeg",
        _ => "image/jpeg"
    };
}
