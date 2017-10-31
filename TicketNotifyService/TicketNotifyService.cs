using Dapper;
using MySql.Data.MySqlClient;
using SharpConfig;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TicketNotifyService.Config;
using TicketNotifyService.Log;
using System.Timers;
using TicketNotifyService.Email;

namespace TicketNotifyService
{
    public partial class TicketNotifyService : ServiceBase
    {
        internal static string ConfigFileName => string.Format(@"{0}\{1}", Program.ExeDir, CONFIG_FILE_NAME);
        private const string CONFIG_FILE_NAME = "config.ini";
        public bool ConsoleMode { get; private set; } = false;
        private static void Log(string log) => _logger.Log(log);
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(TicketNotifyService));
        private ServiceConfig _config;
        private SmtpMailSender _smtp;
        private Timer _timer;

        public int PollRate { get; set; }

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
        //private void TestDapper()
        //{
        //    var sql = "SELECT ost_user_email.address AS `From`, ticket_id AS `TicketId`,  ost_ticket.number AS `TicketNumber`, ost_form.title AS `FormType`, field_id AS `FieldId`, ost_form_field.name AS `FieldVarName`, ost_form_field.label AS `FieldLabel`, ost_form_entry_values.value AS `FieldValue` "+
        //            "FROM ost_ticket " +
        //            "LEFT JOIN ost_form_entry ON ost_ticket.ticket_id = ost_form_entry.object_id " +
        //            "LEFT JOIN ost_form_entry_values ON  ost_form_entry_values.entry_id = ost_form_entry.id " +
        //            "LEFT JOIN ost_form_field ON ost_form_field.id = ost_form_entry_values.field_id " +
        //            "LEFT JOIN ost_form ON ost_form.id = ost_form_field.form_id " +
        //            "LEFT JOIN ost_user_email ON ost_user_email.user_id = ost_ticket.user_id " +
        //            "WHERE status_id = 6";

        //    using (IDbConnection db = new MySqlConnection("Server=10.8.0.13; database=osticket; UID=dev; password=760119"))
        //    {
        //        string readSp = sql;
        //        var result = db.Query<dynamic>(readSp, commandType: CommandType.Text).ToList();
        //        foreach (var row in result)
        //        {
        //            var dict = (IDictionary<string, object>)row;
        //            foreach (var item in dict)
        //            {
        //                Console.WriteLine($"{item.Key}:{item.Value}");
        //            }
        //        }
        //    }
        //}
        protected override void OnStart(string[] args)
        {
            if(!ReadConfig())
            {
                Log("Read config failed -> Exit");
                return;
            }
            Log("Read config OK");
            //general settings & stuff
            Init();
            
            //SMTP settings
            if (!InitSmtp())
            {
                Log("Innit SMTP client failed -> Exit");
                return;
            }
            StartWatcher();
        }
        private void StartWatcher()
        {
            _timer.Start();
            _timer_Elapsed(null, null); //poll now
        }
        private void Init()
        {
            PollRate = _config.PollRate;
            _timer = new Timer(PollRate);
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //skip if still working
            if (_smtp.IsThreadRunning) return;

            //do shit
        }

        private bool InitSmtp()
        {
            try
            {
                _smtp = new SmtpMailSender(_config.DbServer, _config.Port);
                _smtp.SetSmtpAccount(_config.EmailUsername, _config.EmailPwd);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private bool ReadConfig()
        {
            try
            {
                var config = Configuration.LoadFromFile(ConfigFileName);
                _config = new ServiceConfig(config);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected override void OnStop()
        {

        }
    }
}
