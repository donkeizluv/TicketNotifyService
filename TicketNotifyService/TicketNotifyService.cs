using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TicketNotifyService.Log;

namespace TicketNotifyService
{
    public partial class TicketNotifyService : ServiceBase
    {
        internal static string ConfigFileName => string.Format(@"{0}\{1}", Program.ExeDir, CONFIG_FILE_NAME);
        private const string CONFIG_FILE_NAME = "config.ini";
        public bool ConsoleMode { get; private set; } = false;
        private static void Log(string log) => Logger.Log(log);
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(TicketNotifyService));

        public TicketNotifyService()
        {
            InitializeComponent();
        }

        internal void ConsoleStart(string[] args)
        {
            //able to run in 2 modes is cool
            ConsoleMode = true;
            Log("Run in console mode....");
            OnStart(args);
            Console.ReadLine();
        }
        private void TestDapper()
        {
            using (IDbConnection db = new MySqlConnection("Server=localhost; database=devdb; UID=dev; password=760119"))
            {
                string readSp = "select * from test";
                var result = db.Query<dynamic>(readSp, commandType: CommandType.Text).ToList();
                foreach (var row in result)
                {
                    var dict = (IDictionary<string, object>)row;
                    foreach (var item in dict)
                    {
                        Console.WriteLine($"{item.Key}:{item.Value}");
                    }
                }
            }
        }
        protected override void OnStart(string[] args)
        {
            //TestConenction();
            TestDapper();
        }

        protected override void OnStop()
        {

        }
    }
}
