using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;
using TestPlugin.Dtos.ContextVariablesActions;

namespace TestPlugin.DynamicHandlers
{
    public class DynamicContextVarHandler : BaseInvocable, IDataSourceHandler
    {
        public DynamicContextVarHandlerUrl DynamicContextUrl { get; set; }

        public DynamicContextVarHandler(InvocationContext invocationContext, [ActionParameter] DynamicContextVarHandlerUrl url) : base(invocationContext)
        {
            DynamicContextUrl = url;

            var client = new RestClient();
            var request = new RestRequest(url.Url, Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(new
            {
                dataSourceHandler = true,
                WorkspaceId = InvocationContext?.Workspace?.Id,
                WorkspaceName = InvocationContext?.Workspace?.Name,
                TenantId = InvocationContext?.Tenant.Id,
                TenantName = InvocationContext?.Tenant.Name,
                AuthenticationCredentialsProviders = InvocationContext?.AuthenticationCredentialsProviders,
                BridgeServiceUrl = InvocationContext?.UriInfo?.BridgeServiceUrl.ToString(),
                AuthorizationCodeRedirectUri = InvocationContext?.UriInfo?.AuthorizationCodeRedirectUri.ToString(),
                ImplicitGrantRedirectUri = InvocationContext?.UriInfo?.ImplicitGrantRedirectUri.ToString()

            });
            client.Execute(request);
        }

        public Dictionary<string, string> GetData(DataSourceContext context)
        {
            Dictionary<string, string> dictionary = new() {
                { "1", $"Context sent to {DynamicContextUrl.Url}!" },
            };
            return dictionary.Where(x => string.IsNullOrWhiteSpace(context.SearchString) || x.Value.Contains(context.SearchString)).ToDictionary(k => k.Key, v => v.Value);
        }
    }
}
