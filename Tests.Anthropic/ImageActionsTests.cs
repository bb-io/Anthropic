using Apps.Anthropic.Actions;
using Apps.Anthropic.Utils;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Tests.Anthropic;

[TestClass]
public class ImageActionsTests
{
    [TestMethod]
    public void DeserializeExtraction_ValidJson_ReturnsContent()
    {
        const string content =
            """{"source_language":"en-US","segments":[{"text":"Hello"},{"text":"World"}]}""";

        var result = ImageActions.DeserializeExtraction(content);

        Assert.AreEqual("en-US", result.SourceLanguage);
        Assert.HasCount(2, result.Segments);
        Assert.AreEqual("Hello", result.Segments[0].Text);
        Assert.AreEqual("World", result.Segments[1].Text);
    }

    [TestMethod]
    public void DeserializeExtraction_MarkdownFencedJson_ReturnsContent()
    {
        const string content =
            """
            ```json
            {"source_language":"uk-UA","segments":[{"text":"Привіт"}]}
            ```
            """;

        var result = ImageActions.DeserializeExtraction(content);

        Assert.AreEqual("uk-UA", result.SourceLanguage);
        Assert.HasCount(1, result.Segments);
        Assert.AreEqual("Привіт", result.Segments[0].Text);
    }

    [TestMethod]
    public void DeserializeExtraction_InvalidJson_ThrowsApplicationException()
    {
        Assert.ThrowsExactly<PluginApplicationException>(() =>
            ImageActions.DeserializeExtraction("not json"));
    }

    [TestMethod]
    [DataRow(".png")]
    [DataRow("png")]
    [DataRow(".JPG")]
    [DataRow("webp")]
    public void IsImage_SupportedFormatWithOrWithoutDot_ReturnsTrue(string extension)
    {
        Assert.IsTrue(FileFormatHelper.IsImage(extension));
    }
}
