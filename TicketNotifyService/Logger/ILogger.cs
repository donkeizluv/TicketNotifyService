using System;

namespace TicketNotifyService.Log
{
    public interface ILogger
    {
        Type ClassType { get; }
        void Log(string log);
        event OnNewLogHandler OnNewLog;
    }
}