using MimeKit;
using SharpConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketNotifyService.Config
{
    public class ServiceConfig
    {
        private Configuration _config;

        internal static string ScriptFolderPath => string.Format(@"{0}\{1}", Program.ExeDir, SCRIPT_FOLDER);
        private const string SCRIPT_FOLDER = "Scripts";
        //missing:
        //folder to scan for att
        //sent email to?

        //general
        public int PollRate { get; private set; }
        public string AttachmentRootFolder { get; set; }
        public string HelpdeskEmail { get; set; }

        //connection
        public string DbServer { get; private set; }
        public string DbUsername { get; private set; }
        public string DbPwd { get; private set; }
        public string Database { get; private set; }
        public string TablePrefix { get; private set; }
        //scripts
        public string PollScriptFilename { get; private set; }
        public string GetDetailScriptFilename { get; private set; }
        public string UpdateStatusScriptFilename { get; private set; }
        public string GetFilenameScriptFilename { get; private set; }

        //script content
        public string PollScript { get; private set; }
        public string GetDetailScript { get; private set; }
        public string UpdateStatusScript { get; private set; }
        public string GetFilenameScript { get; private set; }


        //status id
        public int StatusToBePolled { get; private set; }
        public int StatusToSet { get; private set; }

        //email
        public string SmtpServer { get; private set; }
        public int Port { get; private set; }
        public string EmailUsername { get; private set; }
        public string EmailPwd { get; private set; }
        public string EmailSuffix { get; private set; }

        public InternetAddress HelpdeskAddress
        {
            get
            {
                return new MailboxAddress(HelpdeskEmail);
            }
        }
        public InternetAddress FromAddress
        {
            get
            {
                var address = EmailUsername.Contains("@") ? EmailUsername : EmailUsername + EmailSuffix;
                return new MailboxAddress(address);
            }
        }

        public const string ConnectionStringTemplate = "Server={server}; database={database}; UID={user}; password={pwd}";
        private const string ServerToken = "{server}";
        private const string DatabaseToken = "{database}";
        private const string DbUserToken = "{user}";
        private const string DbPwdToken = "{pwd}";

        public string ConnectionString
        {
            get
            {
                return ConnectionStringTemplate.Replace(ServerToken, DbServer)
                .Replace(DatabaseToken, Database)
                .Replace(DbUserToken, DbUsername)
                .Replace(DbPwdToken, DbPwd);
            }
        }

        public ServiceConfig(Configuration config)
        {
            _config = config;
            ReadConfig();
        }

        private void ReadConfig()
        {
            var connectionSection = _config["Connection"];
            var scriptSection = _config["Scripts"];
            var statusSection = _config["Status"];
            var emailSection = _config["Email"];
            var genSection = _config["General"];

            //set gen
            PollRate = genSection[nameof(PollRate)].IntValue;
            AttachmentRootFolder = genSection[nameof(AttachmentRootFolder)].StringValueTrimmed;
            HelpdeskEmail = genSection[nameof(HelpdeskEmail)].StringValueTrimmed;

            //set connection
            DbServer = connectionSection[nameof(DbServer)].StringValueTrimmed;
            DbUsername = connectionSection[nameof(DbUsername)].StringValueTrimmed;
            DbPwd = connectionSection[nameof(DbPwd)].StringValueTrimmed;
            Database = connectionSection[nameof(Database)].StringValueTrimmed;
            TablePrefix = connectionSection[nameof(TablePrefix)].StringValueTrimmed;

            //set scripts
            PollScriptFilename = scriptSection[nameof(PollScriptFilename)].StringValueTrimmed;
            GetDetailScriptFilename = scriptSection[nameof(GetDetailScriptFilename)].StringValueTrimmed;
            UpdateStatusScriptFilename = scriptSection[nameof(UpdateStatusScriptFilename)].StringValueTrimmed;
            GetFilenameScriptFilename = scriptSection[nameof(GetFilenameScriptFilename)].StringValueTrimmed;

            //set status
            StatusToBePolled = statusSection[nameof(StatusToBePolled)].IntValue;
            StatusToSet = statusSection[nameof(StatusToSet)].IntValue;

            //set email
            SmtpServer = emailSection[nameof(SmtpServer)].StringValueTrimmed;
            Port = emailSection[nameof(Port)].IntValue;
            EmailUsername = emailSection[nameof(EmailUsername)].StringValueTrimmed;
            EmailPwd = emailSection[nameof(EmailPwd)].StringValueTrimmed;
            EmailSuffix = emailSection[nameof(EmailSuffix)].StringValueTrimmed;

            LoadScripts();
        }
        private void LoadScripts()
        {
            PollScript = File.ReadAllText($"{ScriptFolderPath}\\{PollScriptFilename}")
                     .Replace("{prefix}", TablePrefix)
                     .Replace("{status}", StatusToBePolled.ToString());

            GetDetailScript = File.ReadAllText($"{ScriptFolderPath}\\{GetDetailScriptFilename}")
                .Replace("{prefix}", TablePrefix);


            GetFilenameScript = File.ReadAllText($"{ScriptFolderPath}\\{GetFilenameScriptFilename}")
                .Replace("{prefix}", TablePrefix);

            UpdateStatusScript = File.ReadAllText($"{ScriptFolderPath}\\{UpdateStatusScriptFilename}")
                .Replace("{prefix}", TablePrefix)
                .Replace("{from_status", StatusToBePolled.ToString())
                .Replace("{to_status}", StatusToSet.ToString());
        }
    }
}
