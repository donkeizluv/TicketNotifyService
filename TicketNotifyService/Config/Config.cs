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

        //general
        public int PollRate { get; set; }

        //connection
        public string DbServer { get; set; }
        public string DbUsername { get; set; }
        public string DbPwd { get; set; }
        public string Database { get; set; }
        public string TablePrefix { get; set; }
        //scripts
        public string PollScript { get; set; }
        public string GetDetailScript { get; set; }
        public string UpdateStatusScript { get; set; }
        public string GetFilenameScript { get; set; }
        //status id
        public int StatusToBePolled { get; set; }
        public int StatusToSet { get; set; }

        //email
        public string SmtpServer { get; set; }
        public int Port { get; set; }
        public string EmailUsername { get; set; }
        public string EmailPwd { get; set; }


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
            PollScript = scriptSection[nameof(PollScript)].StringValueTrimmed;
            GetDetailScript = scriptSection[nameof(GetDetailScript)].StringValueTrimmed;
            UpdateStatusScript = scriptSection[nameof(UpdateStatusScript)].StringValueTrimmed;
            GetFilenameScript = scriptSection[nameof(GetFilenameScript)].StringValueTrimmed;

            //set status
            StatusToBePolled = statusSection[nameof(StatusToBePolled)].IntValue;
            StatusToSet = statusSection[nameof(StatusToSet)].IntValue;

            //set email
            SmtpServer = emailSection[nameof(SmtpServer)].StringValueTrimmed;
            Port = emailSection[nameof(Port)].IntValue;
            EmailUsername = emailSection[nameof(EmailUsername)].StringValueTrimmed;
            EmailPwd = emailSection[nameof(EmailPwd)].StringValueTrimmed;

        }

    }
}
