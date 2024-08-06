using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Apps.Anthropic.Models.Request;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;

namespace Apps.Anthropic.DataSourceHandlers
{
    public class GlossaryLanguagesDataHandler : BaseInvocable, IAsyncDataSourceHandler
    {
        private FileReference Glossary { get; set; }
        private IFileManagementClient FileManagementClient;


        public GlossaryLanguagesDataHandler(InvocationContext invocationContext,IFileManagementClient fileManagementClient,[ActionParameter] GlossaryRequest input) : base(invocationContext)
        {
            Glossary = input.Glossary;
            FileManagementClient = fileManagementClient;
        }

        public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
        {
            var glossaryStream = await FileManagementClient.DownloadAsync(Glossary);
            var blackbirdGlossary = await glossaryStream.ConvertFromTbx();
            return blackbirdGlossary.ConceptEntries.SelectMany(x => x.LanguageSections)
                .ToDictionary(y => y.LanguageCode, y => y.LanguageCode);
        }
    }
}
