using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace TestPlugin.Dtos.ContextVariablesActions
{
    public class DataFromContextOutput
    {
        public DateTime InvocationDate { get; set; }

        public long WorkspaceId { get; set; }

        public string WorkspaceName { get; set; }

        public long TenantId { get; set; }

        public string TenantName { get; set; }

        public long BirdId { get; set; }

        public string BirdName { get; set; }

        public string FlightId { get; set; }

        public string FlightUrl { get; set; }

        public IEnumerable<AuthenticationCredentialsProvider> AuthenticationCredentialsProviders { get; set; }

        public string BridgeServiceUrl { get; set; }
        public string AuthorizationCodeRedirectUri { get; set; }
        public string ImplicitGrantRedirectUri { get; set; }

        public DataFromContextOutput()
        {
            
        }

        public DataFromContextOutput(InvocationContext context)
        {
            InvocationDate = context.InvocationDate;
            BirdId = context.Bird.Id;
            BirdName = context.Bird.Name;
            FlightId = context.Flight.Id;
            FlightUrl = context.Flight.Url;
            WorkspaceId = context.Workspace.Id;
            WorkspaceName = context.Workspace.Name;
            TenantId = context.Tenant.Id;
            TenantName = context.Tenant.Name;
            AuthenticationCredentialsProviders = context.AuthenticationCredentialsProviders;
            BridgeServiceUrl = context.UriInfo.BridgeServiceUrl.ToString();
            AuthorizationCodeRedirectUri = context.UriInfo.AuthorizationCodeRedirectUri.ToString();
            ImplicitGrantRedirectUri = context.UriInfo.ImplicitGrantRedirectUri.ToString();
        }
    }
}
