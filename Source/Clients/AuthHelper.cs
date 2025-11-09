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