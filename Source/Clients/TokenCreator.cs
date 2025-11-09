using System;
using D365.Framework.Tools.DataMigration.Configuration;
using D365.Framework.Tools.DataMigration.Helper;

namespace D365.Framework.Tools.DataMigration.Clients
{
    public class TokenCreator
    {

        private string CurrentToken { get; set; }
        private DateTime TokenTimestamp { get; set; } = DateTime.MinValue;
        private CommandOptions CommandOptions { get; }
        private Uri DynamicsUrl { get; }
        private AuthHelper AuthHelper {get;}

        public TokenCreator(CommandOptions o, Uri dynamicsUrl)
        {
            this.CommandOptions = o;
            this.DynamicsUrl = dynamicsUrl;
            this.AuthHelper = string.IsNullOrEmpty(o.TenantId) ? new AuthHelper(dynamicsUrl, o.ClientId) : new AuthHelper(dynamicsUrl, o.ClientId, o.TenantId);
        }

        internal string GetToken()
        {
            if (TokenTimestamp.AddMinutes(45) > DateTime.Now && CurrentToken != null)
            {
                return CurrentToken;
            }
            else
            {
                var accessToken = GetAuthToken(this.CommandOptions);
                CurrentToken = accessToken;
                TokenTimestamp = DateTime.Now;
                return accessToken;
            }
        }

        private string GetAuthToken(CommandOptions o)
        {            
            if (o.AuthType == Constants.AUTH_OFFICE365)
            {
                return this.AuthHelper.AuthenticateUser(o.Username, o.Password).Result;
            }
            else if (o.AuthType == Constants.AUTH_CLIENT_ID)
            {
                return this.AuthHelper.AuthenticateClient(o.TenantId, o.ClientId, o.ClientSecret).Result;
            }
            else if (o.AuthType == Constants.AUTH_OUATH)
            {
                return this.AuthHelper.AuthenticateInteractive().Result;
            }
            else
                throw new NotImplementedException();
        }
    }


}