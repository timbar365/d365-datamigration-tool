using log4net;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using Microsoft.Identity.Client;

namespace D365.Framework.Tools.DataMigration.Clients
{
    public class AuthHelper
    {
        private static ILog Log = LogManager.GetLogger(typeof(AuthHelper));

        private const string DEFAULT_CLIENTID = "184c5532-8e3b-4865-965b-f4f1d6357beb";
        private const string GITHUB_OIDC_AUDIENCE = "api://AzureADTokenExchange";
        private string Resource { get; }
        private string[] Scope { get; }

        private IPublicClientApplication ClientApp;

        public AuthHelper(Uri resource, string clientId, string tenantId = "organizations")
        {
            this.Resource = resource.AbsoluteUri;
            this.Scope = new string[] { $"{this.Resource}.default" };

            this.ClientApp = PublicClientApplicationBuilder.Create(clientId).WithDefaultRedirectUri().WithTenantId(tenantId).Build();
        }

        public async Task<string> AuthenticateClient(string tenantId, string clientId, string clientSecret)
        {
            return await this.Authenticate(
                () => $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                () => $"scope={this.Resource}.default&client_id={clientId}&grant_type=client_credentials&client_secret={clientSecret}");
        }

        /// <summary>
        /// Authenticates using the OIDC token of a GitHub Actions run (workload identity federation).
        /// Requires "permissions: id-token: write" in the workflow and a federated credential on the app registration.
        /// </summary>
        public async Task<string> AuthenticateGitHubOidc(string tenantId, string clientId)
        {
            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentException("GitHubOidc authentication requires --tenantid and --clientid.");
            }

            string assertion = await this.GetGitHubIdToken();
            return await this.Authenticate(
                () => $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                () => $"scope={Uri.EscapeDataString(this.Resource + ".default")}&client_id={Uri.EscapeDataString(clientId)}&grant_type=client_credentials" +
                      $"&client_assertion_type={Uri.EscapeDataString("urn:ietf:params:oauth:client-assertion-type:jwt-bearer")}&client_assertion={Uri.EscapeDataString(assertion)}");
        }

        private async Task<string> GetGitHubIdToken()
        {
            string requestUrl = Environment.GetEnvironmentVariable("ACTIONS_ID_TOKEN_REQUEST_URL");
            string requestToken = Environment.GetEnvironmentVariable("ACTIONS_ID_TOKEN_REQUEST_TOKEN");
            if (string.IsNullOrEmpty(requestUrl) || string.IsNullOrEmpty(requestToken))
            {
                throw new InvalidOperationException("GitHub OIDC is not available. Run inside GitHub Actions with 'permissions: id-token: write'.");
            }

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{requestUrl}&audience={Uri.EscapeDataString(GITHUB_OIDC_AUDIENCE)}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", requestToken);
                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Could not get GitHub OIDC token ({(int)response.StatusCode}): {content}");
                }
                return JObject.Parse(content)["value"].Value<string>();
            }
        }

        /// <summary>
        /// Authenticates using the OIDC token of an Azure DevOps pipeline run (workload identity federation).
        /// Requires an Azure Resource Manager service connection using workload identity federation, and
        /// SYSTEM_ACCESSTOKEN mapped into the step's environment.
        /// </summary>
        public async Task<string> AuthenticateAzureDevOpsWif(string tenantId, string clientId, string serviceConnectionId)
        {
            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentException("AzureDevOpsWif authentication requires --tenantid and --clientid (of the service connection's app registration).");
            }

            string assertion = await this.GetAzureDevOpsIdToken(serviceConnectionId);
            return await this.Authenticate(
                () => $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                () => $"scope={Uri.EscapeDataString(this.Resource + ".default")}&client_id={Uri.EscapeDataString(clientId)}&grant_type=client_credentials" +
                      $"&client_assertion_type={Uri.EscapeDataString("urn:ietf:params:oauth:client-assertion-type:jwt-bearer")}&client_assertion={Uri.EscapeDataString(assertion)}");
        }

        private async Task<string> GetAzureDevOpsIdToken(string serviceConnectionId)
        {
            string requestUri = Environment.GetEnvironmentVariable("SYSTEM_OIDCREQUESTURI");
            string accessToken = Environment.GetEnvironmentVariable("SYSTEM_ACCESSTOKEN");
            if (string.IsNullOrEmpty(serviceConnectionId))
            {
                serviceConnectionId = Environment.GetEnvironmentVariable("AZURESUBSCRIPTION_SERVICE_CONNECTION_ID");
            }
            if (string.IsNullOrEmpty(requestUri) || string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Azure DevOps OIDC is not available. Run inside an Azure Pipeline and map SYSTEM_ACCESSTOKEN: $(System.AccessToken) into the step's env.");
            }
            if (string.IsNullOrEmpty(serviceConnectionId))
            {
                throw new ArgumentException("AzureDevOpsWif authentication requires --serviceconnectionid (or env AZURESUBSCRIPTION_SERVICE_CONNECTION_ID).");
            }

            using (var client = new HttpClient())
            {
                var separator = requestUri.Contains("?") ? "&" : "?";
                var request = new HttpRequestMessage(HttpMethod.Post,
                    $"{requestUri}{separator}api-version=7.1&serviceConnectionId={Uri.EscapeDataString(serviceConnectionId)}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Could not get Azure DevOps OIDC token ({(int)response.StatusCode}): {content}");
                }
                return JObject.Parse(content)["oidcToken"].Value<string>();
            }
        }

        public async Task<string> AuthenticateUser(string username, string password, string clientId = DEFAULT_CLIENTID)
        {
            return await this.Authenticate(
                () => "https://login.microsoftonline.com/common/oauth2/token",
                () => $"resource={this.Resource}&client_id={clientId}&grant_type=password&username={username}&password={password}"
            );
        }

        public async Task<string> AuthenticateInteractive(string clientId = DEFAULT_CLIENTID)
        {
            return (await this.ClientApp.AcquireTokenInteractive(this.Scope).ExecuteAsync()).AccessToken;
        }


        protected async Task<string> Authenticate(Func<string> getTokenEndpoint, Func<string> getBody)
        {
            HttpClient client = new HttpClient();
            string tokenEndpoint = getTokenEndpoint();

            var body = getBody();
            var stringContent = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

            var result = await client.PostAsync(tokenEndpoint, stringContent).ContinueWith<string>((response) =>
            {
                return response.Result.Content.ReadAsStringAsync().Result;
            });

            JObject jobject = JObject.Parse(result);
            if (jobject.ContainsKey("access_token"))
            {
                var token = jobject["access_token"].Value<string>();
                return token;
            }
            else
            {
                throw new Exception(result);
            }
        }
    }
}