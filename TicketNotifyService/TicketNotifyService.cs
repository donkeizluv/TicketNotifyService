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
using System.IO;
using TicketNotifyService.Tickets;
using MimeKit;
using TicketNotifyService.Emails;

namespace TicketNotifyService
{
    public partial class TicketNotifyService : ServiceBase
    {
        internal static string ConfigFileName => string.Format(@"{0}\{1}", Program.ExeDir, CONFIG_FILE_NAME);
        private const string CONFIG_FILE_NAME = "config.ini";
        internal static string ScriptFolderPath => string.Format(@"{0}\{1}", Program.ExeDir, SCRIPT_FOLDER);
        private const string SCRIPT_FOLDER = "Scripts";


        public bool ConsoleMode { get; private set; } = false;
        private static void Log(string log) => _logger.Log(log);
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(TicketNotifyService));
        private ServiceConfig _config;
        private SmtpMailSender _smtp;
        private Timer _timer;

        public int PollRate { get; set; }
        //scripts
        public string PollScript { get; set; }
        public string GetDetailScript { get; set; }
        public string UpdateStatusScript { get; set; }
        public string GetFilenameScript { get; set; }

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
            Log("Loading scripts...");
            if(!LoadScripts())
            {
                Log("Load scripts failed -> Exit");
                return;
            }
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
            if (_smtp.IsThreadRunning) return;

            //do shit
            DoShit();

        }

        private void DoShit()
        {
            using (IDbConnection connection = new MySqlConnection(_config.ConnectionString))
            {
                var ids = GetTicketIds(connection);
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
                    var details = GetTicketDetails(id, connection);
                    try
                    {
                        ticketList.Add(TicketParser.ParseToTicket(details));
                    }
                    catch (InvalidDataException ex)
                    {
                        Log($"Parse ticket exception");
                        Log(ex.Message);
                    }
                }
                //tickets to email
                var mails = new List<MimeMessage>();
                foreach (var ticket in ticketList)
                {
                    mails.Add(EmailParser.ParseToEmail(ticket));
                }
                //send emails
                _smtp.EnqueueEmail(mails);
                _smtp.StartSending();
            }
        }
        
        private const string TicketIdToken = "{ticket_id}";
        private List<IDictionary<string, object>> GetTicketDetails(int ticketId, IDbConnection connection)
        {
            var list = new List<IDictionary<string, object>>();
            var result = connection.Query<dynamic>(GetDetailScript.Replace(TicketIdToken, ticketId.ToString()), commandType: CommandType.Text);
            foreach (var item in result)
            {
                list.Add((IDictionary<string, object>)item);
            }
            return list;
        }

        private const string PollStatusToken = "{status}";
        private IEnumerable<int> GetTicketIds(IDbConnection connection)
        {
            var list = new List<int>();
            var result = connection.Query<dynamic>(PollScript.Replace(PollStatusToken, _config.StatusToBePolled.ToString()), commandType: CommandType.Text);
            foreach (var row in result)
            {
                var dict = (IDictionary<string, object>)row;
                foreach (var item in dict)
                {
                    var v = item.Value;
                    if (v == null) continue;

                    if(!int.TryParse(v.ToString(), out var value))
                    {
                        Log($"Cant parse ticket_id: {item.Value.ToString()}");
                        continue;
                    }
                    list.Add(value);
                }
            }
            return list;
        }
        private const string ToStatusToken = "{to_status}";
        //may need to use Execute instead of query
        private void SetStatus(IDbConnection connection, int ticketId)
        {
            var result = connection.Query<dynamic>(UpdateStatusScript.Replace(ToStatusToken, _config.StatusToSet.ToString())
                .Replace(TicketIdToken, ticketId.ToString()), commandType: CommandType.Text);
        }
        private const string FileIdToken = "{file_id}";
        private string GetFileId(IDbConnection connection, int fileId)
        {
            return connection.QueryFirst<string>(GetFilenameScript.Replace(FileIdToken, fileId.ToString()), commandType: CommandType.Text);
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
                Log(ex.Message);
                return false;
            }
        }

        private bool LoadScripts()
        {
            try
            {
                PollScript = File.ReadAllText($"{ScriptFolderPath}\\{_config.PollScriptFilename}")
                    .Replace("{prefix}", _config.TablePrefix)
                    .Replace("{status}", _config.StatusToBePolled.ToString());

                GetDetailScript = File.ReadAllText($"{ScriptFolderPath}\\{_config.GetDetailScriptFilename}")
                    .Replace("{prefix}", _config.TablePrefix);


                GetFilenameScript = File.ReadAllText($"{ScriptFolderPath}\\{_config.GetFilenameScriptFilename}")
                    .Replace("{prefix}", _config.TablePrefix);

                UpdateStatusScript = File.ReadAllText($"{ScriptFolderPath}\\{_config.UpdateStatusScriptFilename}")
                    .Replace("{prefix}", _config.TablePrefix)
                    .Replace("{from_status", _config.StatusToBePolled.ToString())
                    .Replace("{to_status}", _config.StatusToSet.ToString());

                return true;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return false;
            }
        }

        protected override void OnStop()
        {

        }
    }
}
