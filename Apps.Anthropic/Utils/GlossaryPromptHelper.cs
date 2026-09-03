using System.Text;
using System.Text.RegularExpressions;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;

namespace Apps.Anthropic.Utils;

public static class GlossaryPromptHelper
{
    public static async Task<string?> GetGlossaryPromptPart(
        GlossaryRequest input,
        IFileManagementClient fileManagementClient)
    {
        var context = await CreateContextAsync(input.Glossary, fileManagementClient);
        return context?.BuildPrompt([], false);
    }

    internal static async Task<GlossaryPromptContext?> CreateContextAsync(
        FileReference? glossary,
        IFileManagementClient fileManagementClient)
    {
        if (glossary is null)
        {
            return null;
        }

        await using var stream = await fileManagementClient.DownloadAsync(glossary);

        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            bytes = bytes[3..];
        }

        await using var sanitizedStream = new MemoryStream(bytes);
        var blackbirdGlossary = await sanitizedStream.ConvertFromTbx();

        return new GlossaryPromptContext(blackbirdGlossary);
    }
}

internal sealed class GlossaryPromptContext(Glossary glossary)
{
    internal string? BuildPrompt(IEnumerable<string?> content, bool filter)
    {
        var searchableContent = content
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray();

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine(
            "Glossary entries (each entry includes terms in different languages. Each language may have a few synonymous variations which are separated by ;;):");

        var entriesIncluded = false;
        foreach (var entry in glossary.ConceptEntries)
        {
            var terms = entry.LanguageSections.SelectMany(section => section.Terms.Select(term => term.Term));
            if (!ShouldIncludeEntry(terms, searchableContent, filter))
            {
                continue;
            }

            entriesIncluded = true;
            sb.AppendLine();
            sb.AppendLine("\tEntry:");

            foreach (var section in entry.LanguageSections)
            {
                sb.AppendLine(
                    $"\t\t{section.LanguageCode}: {string.Join(";; ", section.Terms.Select(term => term.Term))}");
            }
        }

        return entriesIncluded ? sb.ToString() : null;
    }

    internal static bool ShouldIncludeEntry(
        IEnumerable<string?> terms,
        IReadOnlyCollection<string> content,
        bool filter)
    {
        return !filter || terms.Any(term => ContainsWholeTerm(content, term));
    }

    private static bool ContainsWholeTerm(IReadOnlyCollection<string> content, string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        var pattern = $@"(?<![\p{{L}}\p{{N}}_]){Regex.Escape(term.Trim())}(?![\p{{L}}\p{{N}}_])";
        return content.Any(value => Regex.IsMatch(
            value,
            pattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }
}