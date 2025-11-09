using CommandLine;
using D365.Framework.Tools.DataMigration.Helper;

namespace D365.Framework.Tools.DataMigration.Configuration
{
    public class CommandOptions
    {
        [Option('a', "authtype", Required = false, HelpText = "e.g. Office365, ClientId", Default = Constants.AUTH_OFFICE365)]
		public string AuthType
		{
			get;
			set;
		}

		[Option("url", Required = true, HelpText = "Url of Dynamics 365")]
		public string Url
		{
			get;
			set;
		}

		[Option('u', "user", Required = false, HelpText = "Username")]
		public string Username
		{
			get;
			set;
		}

		[Option('p', "password", Required = false, HelpText = "Password")]
		public string Password
		{
			get;
			set;
		}

		[Option('t', "tenantid", Required = false, HelpText = "TenantId")]
		public string TenantId
		{
			get;
			set;
		}

		[Option('c', "clientid", Required = false, HelpText = "ClientId or AppId")]
		public string ClientId
		{
			get;
			set;
		}

		[Option('s', "clientsecret", Required = false, HelpText = "ClientSecret")]
		public string ClientSecret
		{
			get;
			set;
		}

		[Option('f', "file", Required = true, HelpText = "Data file to import or where to save export")]
		public string File
		{
			get;
			set;
		}

		[Option('e', "expconf", Required = false, HelpText = "File to export configuration")]
		public string ExportConfigurationFilePath
		{
			get;
			set;
		}

		[Option('o', "ownermap", Required = false, HelpText = "File to owner mapping")]
		public string OwnerMappingFilePath
		{
			get;
			set;
		}

		[Option('m', "mode", Required = true, HelpText = "import or export")]
		public string Mode
		{
			get;
			set;
		}
		
		[Option('v', "verbose", Required = false, HelpText = "Should details be logged", Default = false)]
		public bool Verbose
		{
			get;
			set;
		}
		
		[Option("returnError", Required = false, HelpText = "True - Return code -1 if any error", Default=false)]
		public bool ReturnError
		{
			get;
			set;
		}
		
		[Option("timeout", Required = false, HelpText = "Web request timeout in ms", Default=100000)]
		public int Timeout
		{
			get;
			set;
		}

		[Option("parallelLimit", Required = false, HelpText = "Number of parallel threads", Default=5)]
		public int ParallelLimit
		{
			get;
			set;
		}

		[Option("fieldimportmap", Required = false, HelpText = "File to import field mapping")]
		public string ImportMappingFilePath
		{
			get;
			set;
		}

		public void CheckIsValid(){
			//TODO throw exception if not valid
			;
		}
    }
}