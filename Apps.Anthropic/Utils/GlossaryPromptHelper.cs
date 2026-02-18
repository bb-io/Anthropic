using System.Text;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;

namespace Apps.Anthropic.Utils;

public static class GlossaryPromptHelper
{
    public static async Task<string> GetGlossaryPromptPart(GlossaryRequest input, IFileManagementClient fileManagementClient)
    {
        if (input.Glossary is null)
            return null;

        var stream = await fileManagementClient.DownloadAsync(input.Glossary);

        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            bytes = bytes[3..];

        await using var sanitizedStream = new MemoryStream(bytes);
        var blackbirdGlossary = await sanitizedStream.ConvertFromTbx();

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine(
            "Glossary entries (each entry includes terms in different languages. Each language may have a few synonymous variations which are separated by ;;):");

        foreach (var entry in blackbirdGlossary.ConceptEntries)
        {
            sb.AppendLine();
            sb.AppendLine("\tEntry:");

            foreach (var section in entry.LanguageSections)
            {
                sb.AppendLine(
                    $"\t\t{section.LanguageCode}: {string.Join(";; ", section.Terms.Select(t => t.Term))}");
            }
        }

        return sb.ToString();
    }
}