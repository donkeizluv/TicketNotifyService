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
using TicketNotifyService.Emails;
using System.IO;
using TicketNotifyService.Tickets;
using MimeKit;
using TicketNotifyService.Emails;
using TicketNotifyService.SQL;

namespace TicketNotifyService
{
    public partial class TicketNotifyService : ServiceBase
    {
        internal static string ConfigFileName => string.Format(@"{0}\{1}", Program.ExeDir, CONFIG_FILE_NAME);
        private const string CONFIG_FILE_NAME = "config.ini";

        
        private static void Log(string log) => _logger.Log(log);
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(TicketNotifyService));
        private ServiceConfig _config;
        private SmtpMailSender _smtp;
        private Timer _timer;

        public bool ConsoleMode { get; private set; } = false;

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
            //_timer.Start();
            _timer_Elapsed(null, null); //poll now
        }

        private void Init()
        {
            EmailParser.Config = _config;
            PollRate = _config.PollRate;
            _timer = new Timer(PollRate);
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //skip if still working
            if (_smtp.IsThreadRunning)
            {
                Log("Thread is still running -> Skip");
                return;
            }
            //do shit
            Do();

        }

        private void Do()
        {
            //keep this SqlWrapper instance for the whole life off service?
            using (var sql = new SqlWrapper(_config))
            {
                var ids = sql.GetTicketIds();
                Log($"Tickets matched status count: {ids.Count()}");
                if(ids.Count() < 1)
                {
                    Log("Nothing to do....");
                    return;
                }
                //parse matched tickets
                var ticketList = new List<Ticket>();
                foreach (var id in ids)
                {
                    //get ticket details
                    var details = sql.GetTicketDetails(id);
                    try
                    {
                        ticketList.Add(TicketParser.ParseToTicket(details));
                    }
                    catch (InvalidDataException ex)
                    {
                        Log($"Parse ticket exception");
                        Log(ex.Message);
                        Log(ex.StackTrace);
                    }
                    catch (InvalidCastException ex)
                    {
                        Log($"Parse ticket exception");
                        Log(ex.Message);
                        Log(ex.StackTrace);
                    }
                }
                //tickets to email
                var mails = new List<MimeMessage>();
                foreach (var ticket in ticketList)
                {
                    mails.Add(EmailParser.ParseToEmail(sql, ticket));
                }
                //send emails
                _smtp.EnqueueEmail(mails);
            }
            _smtp.StartSending();
        }
        
        

        private bool InitSmtp()
        {
            try
            {
                _smtp = new SmtpMailSender(_config.SmtpServer, _config.Port);
                _smtp.SetSmtpAccount(_config.EmailUsername, _config.EmailPwd);
                return true;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                Log(ex.StackTrace);
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
            catch (Exception ex)
            {
                Log(ex.GetType().ToString());
                Log(ex.Message);
                Log(ex.StackTrace);
                return false;
            }
        }

        protected override void OnStop()
        {

        }
    }
}
