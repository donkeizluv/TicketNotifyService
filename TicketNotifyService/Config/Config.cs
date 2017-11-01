using MimeKit;
using SharpConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketNotifyService.Config
{
    public class ServiceConfig
    {
        private Configuration _config;

        //missing:
        //folder to scan for att
        //sent email to?

        //general
        public int PollRate { get; set; }

        //connection
        public string DbServer { get; set; }
        public string DbUsername { get; set; }
        public string DbPwd { get; set; }
        public string Database { get; set; }
        public string TablePrefix { get; set; }
        //scripts
        public string PollScriptFilename { get; set; }
        public string GetDetailScriptFilename { get; set; }
        public string UpdateStatusScriptFilename { get; set; }
        public string GetFilenameScriptFilename { get; set; }
        //status id
        public int StatusToBePolled { get; set; }
        public int StatusToSet { get; set; }

        //email
        public string SmtpServer { get; set; }
        public int Port { get; set; }
        public string EmailUsername { get; set; }
        public string EmailPwd { get; set; }
        public string EmailSuffix { get; set; }

        //status
        public string HelpdeskEmail = "helpdesk@hdsaison.com.vn";
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

        }

    }
}
