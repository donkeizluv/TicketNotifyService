using System;

namespace TicketNotifyService.Log
{
    public class SimpleLogger : ILogger
    {
        public SimpleLogger(Type type)
        {
            ClassType = type;
        }

        public Type ClassType { get; }
        public event OnNewLogHandler OnNewLog;

        public void Log(string log)
        {
            RaiseNewLogEvent(log);
        }

        private void RaiseNewLogEvent(string log)
        {
            OnNewLog?.Invoke(this, new NewLogEventArgs(log));
        }
    }
}