using System;
using System.IO;
using System.Reflection;
using CommandLine;
using D365.Framework.Tools.DataMigration.Clients;
using D365.Framework.Tools.DataMigration.Configuration;
using D365.Framework.Tools.DataMigration.Export;
using D365.Framework.Tools.DataMigration.Helper;
using D365.Framework.Tools.DataMigration.Import;
using D365.Framework.Tools.DataMigration.Types;
using log4net;
using log4net.Config;

namespace D365.Framework.Tools.DataMigration
{
    class Program
    {
        static ILog Log = LogManager.GetLogger(typeof(Program));

        static int Main(string[] args)
        {
            try
            {
                var appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Replace("file:\\", "", true, null);
                // System.Console.WriteLine(appPath);
                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo(Path.Combine(appPath, "log4net.config")));
                Log.Debug("Starting");

                Parser.Default.ParseArguments<CommandOptions>(args)
                       .WithParsed<CommandOptions>(o =>
                       {
                           //Additional checks
                           o.CheckIsValid();

                           //Increase logging
                           if (o.Verbose) ((log4net.Repository.Hierarchy.Logger)Log.Logger).Parent.Level = log4net.Core.Level.Debug;

                           Log.Debug("Connecting to Dynamics");
                           var dynamicsUrl = new Uri(o.Url);

                           System.Net.ServicePointManager.DefaultConnectionLimit = int.MaxValue;
                           var d365Client = GetDynamicsClient(o, dynamicsUrl);

                           if (o.Mode?.ToLower() == "export")
                           {
                               var exporter = new DataExporter(d365Client);
                               exporter.Export(o.ExportConfigurationFilePath, o.File);
                           }
                           else if (o.Mode?.ToLower() == "import")
                           {
                               var importer = new DataImporter(d365Client, o.ParallelLimit, new OwnerMapper(o.OwnerMappingFilePath), new FieldMapper(o.ImportMappingFilePath, d365Client));
                               var allSuccessfull = importer.Import(o.File);
                               if (!allSuccessfull && o.ReturnError) throw new Exception("Errors occurred. Please check log for details.");
                           }
                           else
                           {
                               throw new ArgumentOutOfRangeException("mode", "Mode must be either export or import");
                           }
                       }).WithNotParsed(errs =>
                       {
                           throw new ArgumentException("Commandline parameters not correct.");
                       });
                Log.Info("Done");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Log.Error(ex);
                return -1;
            }
        }

        private static DynamicsClient GetDynamicsClient(CommandOptions o, Uri dynamicsUrl)
        {
            return new DynamicsClient(dynamicsUrl, new TokenCreator(o, dynamicsUrl), o.Timeout);
        }

    }
}
