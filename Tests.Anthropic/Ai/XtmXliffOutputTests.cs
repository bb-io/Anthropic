using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Apps.Anthropic.Actions;
using Apps.Anthropic.Ai;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Request.Optional;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Blackbird.Applications.SDK.Blueprints.Handlers;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Tests.Anthropic.Ai;

[TestClass]
public class XtmXliffOutputTests
{
    internal const string Xliff12Namespace = "urn:oasis:names:tc:xliff:document:1.2";
    internal const string Xliff1OutputHandling = "xliff1";
    internal const string SkeletonContent = "c2tlbGV0b24tcGxhY2Vob2xkZXI=";

    internal const string XtmStyleXliff12 = $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:xtm="urn:xliff-xtm-extensions" version="1.2">
          <file source-language="en-US" target-language="es-ES" datatype="xml" original="file-1" tool="xtm-intl_XLIFF_Extract" xtm:populate-target-with-source="yes">
            <header><skl><internal-file crc="00000000">{SkeletonContent}</internal-file></skl></header>
            <body>
              <group id="g1">
                <trans-unit id="t1" translate="yes">
                  <source>Test document title</source>
                  <target state="translated" state-qualifier="mt-suggestion">Título del documento de prueba</target>
                </trans-unit>
              </group>
              <group id="g2">
                <trans-unit id="t2" translate="yes">
                  <source>Internal use only</source>
                  <target state="translated" state-qualifier="mt-suggestion">Solo para uso interno</target>
                </trans-unit>
              </group>
            </body>
          </file>
        </xliff>
        """;

    private static readonly Regex PromptItemId = new("\"id\"\\s*:\\s*\"(?<id>[^\"]+)\"", RegexOptions.Compiled);

    private readonly InMemoryFileManagementClient _files = new();

    [TestMethod]
    [DataRow(typeof(TranslateContentRequest))]
    [DataRow(typeof(EditContentRequest))]
    [DataRow(typeof(ReviewContentRequest))]
    [DataRow(typeof(BackgroundDownloadRequest))]
    public void OutputFileHandling_OffersXliff12_AndKeepsExistingOptions(Type requestType)
    {
        var offered = OutputFileHandlingOptions(requestType);
        var sdkDefaults = new ProcessFileFormatHandler().GetData().Select(item => item.Value);

        CollectionAssert.Contains(offered, Xliff1OutputHandling);
        CollectionAssert.IsSubsetOf(sdkDefaults.ToList(), offered);
    }

    [TestMethod]
    public async Task Edit_XtmXliff12WithXliff1Handling_ReturnsXliff12WithEditedTargetsAndSkeleton()
    {
        var file = _files.Add("es_ES_document.docx.xlf", XtmStyleXliff12);
        var actions = new FakeProviderEditActions(Context(), _files, new RecordingLlmClient(EditEverySegment));

        var result = await actions.EditContent(
            new ModelIdentifier { Model = "claude-sonnet-5" },
            new EditContentRequest
            {
                File = file,
                TargetLanguage = "es-ES",
                OutputFileHandling = Xliff1OutputHandling,
                GenerateDocumentSummary = false
            },
            new OptionalSkillRequest());

        var output = XDocument.Parse(_files.Read(result.File));
        XNamespace xliff = Xliff12Namespace;
        var units = output.Descendants(xliff + "trans-unit").ToList();

        Assert.AreEqual("es_ES_document.docx.xlf", result.File.Name);
        Assert.AreEqual("1.2", output.Root!.Attribute("version")?.Value);
        Assert.AreEqual(Xliff12Namespace, output.Root.Name.NamespaceName);
        Assert.AreEqual(SkeletonContent, output.Descendants(xliff + "internal-file").Single().Value.Trim());
        CollectionAssert.AreEqual(new[] { "t1", "t2" }, units.Select(unit => unit.Attribute("id")!.Value).ToArray());
        Assert.IsTrue(units.All(unit => unit.Element(xliff + "target")!.Value.StartsWith("Editado")),
            output.ToString());
        Assert.AreEqual(2, result.TotalSegmentsUpdated);
    }

    private static List<string> OutputFileHandlingOptions(Type requestType)
    {
        var dataSource = requestType.GetProperty("OutputFileHandling")!.CustomAttributes
            .Single(attribute => attribute.AttributeType == typeof(StaticDataSourceAttribute));
        var handlerType = (Type)dataSource.ConstructorArguments[0].Value!;
        var handler = (IStaticDataSourceItemHandler)Activator.CreateInstance(handlerType)!;
        return handler.GetData().Select(item => item.Value).ToList();
    }

    private static string EditEverySegment(LlmRequest request)
    {
        var edits = PromptItemId.Matches(request.UserPrompt)
            .Select(match => $$"""{ "translation_id": "{{match.Groups["id"].Value}}", "translated_text": "Editado {{match.Groups["id"].Value}}" }""");
        return $$"""{ "translations": [{{string.Join(",", edits)}}] }""";
    }

    private static InvocationContext Context() => new()
    {
        AuthenticationCredentialsProviders =
        [
            new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.AnthropicNative),
            new AuthenticationCredentialsProvider(CredNames.ApiKey, "unused")
        ],
        Logger = new Logger("Debug")
    };

    private sealed class FakeProviderEditActions(InvocationContext context, IFileManagementClient files, ILlmClient provider)
        : EditActions(context, files)
    {
        protected override ILlmClient CreateLlmClient(string? model, AnthropicLlmSettings settings) => provider;
    }
}
