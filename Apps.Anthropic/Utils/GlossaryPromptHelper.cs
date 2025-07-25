using System.Text;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;

namespace Apps.Anthropic.Utils;

public static class GlossaryPromptHelper
{
    public static async Task<string> GetGlossaryPromptPart(GlossaryRequest input, IFileManagementClient fileManagementClient)
    {
        var glossaryStream = await fileManagementClient.DownloadAsync(input.Glossary);
        var blackbirdGlossary = await glossaryStream.ConvertFromTbx();

        var glossaryPromptPart = new StringBuilder();
        glossaryPromptPart.AppendLine();
        glossaryPromptPart.AppendLine(
            "Glossary entries (each entry includes terms in different languages. Each language may have a few synonymous variations which are separated by ;;):");

        
        foreach (var entry in blackbirdGlossary.ConceptEntries)
        {
            
            glossaryPromptPart.AppendLine();
            glossaryPromptPart.AppendLine("\tEntry:");

            foreach (var section in entry.LanguageSections)
            {
                glossaryPromptPart.AppendLine(
                    $"\t\t{section.LanguageCode}: {string.Join(";; ", section.Terms.Select(term => term.Term))}");
            }
        }

        return glossaryPromptPart.ToString();
    }
}