using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Microsoft.Extensions.Configuration;

namespace Tests.Anthropic.Base;

public class TestBase
{
    public IEnumerable<AuthenticationCredentialsProvider> Creds { get; set; }
    public List<IEnumerable<AuthenticationCredentialsProvider>> CredentialGroups { get; private set; }

    public InvocationContext InvocationContext { get; set; }
    public List<InvocationContext> InvocationContexts { get; private set; }

    public FileManager FileManager { get; set; }

    public TestContext? TestContext { get; set; }

    public TestBase()
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        Creds = config.GetSection("ConnectionDefinition").GetChildren().Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value)).ToList();
        var folderLocation = config.GetSection("TestFolder").Value;

        InvocationContext = new InvocationContext
        {
            AuthenticationCredentialsProviders = Creds,
        };

        InitializeCredentials();
        InitializeInvocationContext();
        InitializeFileManager();
    }

    private void InitializeCredentials()
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        CredentialGroups = config.GetSection("ConnectionDefinition")
            .GetChildren()
            .Select(section =>
                section.GetChildren()
               .Select(child => new AuthenticationCredentialsProvider(child.Key, child.Value))
            )
            .ToList();
    }

    private void InitializeInvocationContext()
    {
        InvocationContexts = new List<InvocationContext>();
        foreach (var credentialGroup in CredentialGroups)
        {
            InvocationContexts.Add(new InvocationContext
            {
                AuthenticationCredentialsProviders = credentialGroup
            });
        }
    }

    private void InitializeFileManager()
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var folderLocation = config.GetSection("TestFolder").Value;
        FileManager = new FileManager(folderLocation!);
    }
}