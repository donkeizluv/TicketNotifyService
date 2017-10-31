using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using TicketNotifyService.Log;

namespace TicketNotifyService
{
    static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            if (Environment.UserInteractive || ForceConsole(args))
            {
                var service = new TicketNotifyService();
                LogManager.IsConsole = true;
                service.ConsoleStart(args);
            }
            else
            {
                var servicesToRun = new ServiceBase[]
                {
                    new TicketNotifyService()
                };
                ServiceBase.Run(servicesToRun);
            }
        }
        private static bool ForceConsole(string[] args)
        {
            foreach (var a in args)
            {
                return string.Compare(a, "-console", true) == 0;
            }
            return false;
        }
        internal static string ExeDir
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
