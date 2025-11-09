using System.Net;

namespace D365.Framework.Tools.DataMigration.Clients
{
    public class WebClientTimeout : WebClient
    {
        private int Timeout {get;set;}

        public WebClientTimeout(int timeout)
        {
            this.Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(System.Uri address) {
            WebRequest wr = base.GetWebRequest(address);
            wr.Timeout = this.Timeout;
            return wr;
        }        
    }
}