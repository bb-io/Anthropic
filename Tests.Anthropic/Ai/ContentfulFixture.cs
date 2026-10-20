using System.Globalization;
using System.Text.RegularExpressions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Transformations;

namespace Tests.Anthropic.Ai;

// The repository's contentful fixtures, read through the test file manager and changed only in memory.
public static class ContentfulFixture
{
    public const string Html = "contentful.html";
    public const string Xliff = "contentful.html.xlf";
    public const int XliffSegments = 47;

    public static async Task<string> ReadAsync(IFileManagementClient files, string name)
    {
        await using var stream = await files.DownloadAsync(new FileReference { Name = name });
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    // The contentful fixtures declare version="2,2", which Blackbird.Filters (before and after the
    // migration) does not recognise as XLIFF 2. The fixture is left as it is; only the copy is fixed.
    public static async Task<string> ReadXliffAsync(IFileManagementClient files) =>
        (await ReadAsync(files, Xliff)).Replace("version=\"2,2\"", "version=\"2.2\"");

    // Replaces the target of every segment with this source; the escaped copy in <skeleton> is not matched.
    public static string PlantTarget(string xliff, string source, string plantedTarget)
    {
        var pattern = new Regex($@"(<source>{Regex.Escape(source)}</source>\s*<target>)[^<]*(</target>)");
        Assert.IsGreaterThan(0, pattern.Matches(xliff).Count, $"No segment with source '{source}'.");
        return pattern.Replace(xliff, $"${{1}}{plantedTarget}${{2}}");
    }

    public static double? Score(Segment segment)
    {
        var value = segment.TargetAttributes.SingleOrDefault(attribute => attribute.Name.LocalName == "extradata")?.Value;
        return value is null ? null : double.Parse(value, CultureInfo.InvariantCulture);
    }
}
