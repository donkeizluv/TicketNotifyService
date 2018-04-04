using SharpConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using TicketNotifyService.Config;
using TicketNotifyService.Log;
using System.Timers;
using TicketNotifyService.Emails;
using System.IO;
using TicketNotifyService.Tickets;
using MimeKit;
using System;
using TicketNotifyService.SQL;

namespace TicketNotifyService
{
    public partial class TicketNotifyService : ServiceBase
    {
        internal static string ConfigFileName => string.Format(@"{0}\{1}", Program.ExeDir, CONFIG_FILE_NAME);
        internal static string ConfigJSONMap => string.Format(@"{0}\{1}", Program.ExeDir, CONFIG_JSON_MAP);
        private const string CONFIG_FILE_NAME = "config.ini";
        private const string CONFIG_JSON_MAP = "map.json";


        private static void Log(string log, bool isVerbose = false)
        {
            if (OutputVerboseLog)
            {
                _logger.Log(log);
            }
            else
            {
                if (!isVerbose)
                    _logger.Log(log);
            }

        }

        private static readonly ILogger _logger = LogManager.GetLogger(typeof(TicketNotifyService));
        private ServiceConfig _config;
        private SmtpMailSender _smtp;
        private Timer _timer;
        public static bool OutputVerboseLog { get; set; }

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
#if !DEBUG
            _timer.Start();
#endif
            _timer_Elapsed(null, null); //poll now
        }

        private void Init()
        {
            EmailComposer.Config = _config;
            PollRate = _config.PollRate;
            _timer = new Timer(PollRate);
            _timer.Elapsed += _timer_Elapsed;
            OutputVerboseLog = _config.VerboseLog;
        }

        private void _smtp_OnEmailSendingThreadExit(object sender, EmailSendingThreadEventArgs e)
        {
            Log("Sent completed");
            _timer.Start(); //start timer again when done
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //TODO: monitor ex overtime to catch specific ex
            try
            {
                //skip if still working
                if (_smtp.IsThreadRunning)
                {
                    Log("Thread or routine is still running -> Skip");
                    return;
                }
                //do shit
                _timer.Stop(); //prevent elapse
                Do();
            }
            catch (Exception ex)
            {
                Log("Exception in routine:");
                Log(ex.Message);
                Log(ex.StackTrace);

                if(ex.InnerException != null)
                {
                    var inner = ex.InnerException;
                    Log("Inner Ex:");
                    Log(inner.Message);
                    Log(inner.StackTrace);
                }
            }
        }

        private void Do()
        {
            //keep this SqlWrapper instance for the whole life off service?
            using (var sql = new SqlWrapper(_config))
            {
                var ids = sql.GetTicketIds();
                if(ids.Count() < 1)
                {
                    Log("Nothing to do....", true);
                    _timer.Start();
                    return;
                }
                Log($"Tickets matched status count: {ids.Count()}");
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
                    using (var parser = new EmailComposer(sql, ticket, _config))
                    {
                        //try pattern
                        //or cant fail here?
                        mails.Add(parser.ToEmailMessage());
                    }
                }
                //send emails
                _smtp.EnqueueEmail(mails);
                //set status away
                ids.ToList().ForEach(id => sql.SetStatus(id));
            }
            Log("Start sending");
            _smtp.StartSending();
        }
        

        private bool InitSmtp()
        {
            try
            {
                _smtp = new SmtpMailSender(_config.SmtpServer, _config.Port);
                _smtp.SetSmtpAccount(_config.EmailUsername, _config.EmailPwd);
                _smtp.OnEmailSendingThreadExit += _smtp_OnEmailSendingThreadExit;
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
                TryGetJsonMap(out var jsonMap);
                _config = new ServiceConfig(config, jsonMap);
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
        private bool TryGetJsonMap(out string json)
        {
            json = string.Empty;
            try
            {
                json = File.ReadAllText($"{ConfigJSONMap}");
                return true;
            }
            catch
            {
                return false;
            }
        }
        protected override void OnStop()
        {

        }
    }
}
