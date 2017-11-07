using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TicketNotifyService.Log
{
    public static class LogManager
    {
        private const string LogFileName = "log.txt";
        private static readonly List<ILogger> ListLogger = new List<ILogger>();
        private static readonly object WriteLogLocker = new object();


        static LogManager()
        {
            IsConsole = false;
        }

        private static string LogPath => string.Format(@"{0}\{1}", Program.ExeDir, LogFileName);
        public static bool IsConsole { get; set; }

        public static ILogger GetLogger(Type t)
        {
            var logger = new SimpleLogger(t);
            ListLogger.Add(logger);
            logger.OnNewLog += Logger_OnNewLog;
            return logger;
        }

        private static void Logger_OnNewLog(ILogger log, NewLogEventArgs e)
        {
            if (e.Log == string.Empty) return;
            lock (WriteLogLocker)
            {
                WriteLog(e.Log);
            }
        }

        private static void WriteLog(string log)
        {
            if (IsConsole)
                Console.WriteLine(FormatLog(log));
            //write new log
            File.AppendAllLines(LogPath, new string[] { FormatLog(log) }, Encoding.UTF8);
        }

        private static string FormatLog(string log)
        {
            return string.Format("{0:G} - {1}", DateTime.Now, log);
        }
    }
}