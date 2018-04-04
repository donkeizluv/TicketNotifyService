using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TicketNotifyService.Config;
using TicketNotifyService.Log;

namespace TicketNotifyService.SQL
{
    public class SqlWrapper : IDisposable
    {
        private static void Log(string log) => _logger.Log(log);
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(SqlWrapper));

        private ServiceConfig _config;
        private IDbConnection _connection;
        public SqlWrapper(ServiceConfig config)
        {
            _config = config;
            _connection = new MySqlConnection(_config.ConnectionString);
        }
        private const string TicketIdToken = "{ticket_id}";
        public List<IDictionary<string, object>> GetTicketDetails(int ticketId)
        {
            var list = new List<IDictionary<string, object>>();
            var result = _connection.Query<dynamic>(_config.GetDetailScript.Replace(TicketIdToken, ticketId.ToString()), commandType: CommandType.Text);
            foreach (var item in result)
            {
                list.Add((IDictionary<string, object>)item);
            }
            return list;
        }

        private const string PollStatusToken = "{status}";
        public IEnumerable<int> GetTicketIds()
        {
            var list = new List<int>();
            var result = _connection.Query<dynamic>(_config.PollScript.Replace(PollStatusToken, _config.StatusToBePolled.ToString()), commandType: CommandType.Text);
            foreach (var row in result)
            {
                var dict = (IDictionary<string, object>)row;
                foreach (var item in dict)
                {
                    var v = item.Value;
                    if (v == null) continue;

                    if (!int.TryParse(v.ToString(), out var value))
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
        //Set status flag to "Notified"
        public void SetStatus(int ticketId)
        {
            //may need to use Execute instead of query
            var result = _connection.Query<dynamic>(_config.UpdateStatusScript.Replace(ToStatusToken, _config.StatusToSet.ToString())
                .Replace(TicketIdToken, ticketId.ToString()), commandType: CommandType.Text);
        }

        private const string FileIdToken = "{file_id}";
        public string GetFilename(string fileId)
        {
            return _connection.QueryFirstOrDefault<string>(_config.GetFilenameScript.Replace(FileIdToken, fileId.ToString()), commandType: CommandType.Text);
        }
        //~SqlWrapper()
        //{

        //}
        public void Dispose()
        {
            if(_config != null)
            {
                _config = null;
            }
            if(_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
            }
        }
    }
}
