using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using TicketNotifyService.Log;

namespace TicketNotifyService.Emails
{
    public delegate void EmailSendingThreadExitEventHandler(object sender, EmailSendingThreadEventArgs e);

    public delegate void EmailSendingProgressChangedEventHandler(object sender, EmailSendingProgressChangedArgs e);

    public class SmtpMailSender
    {
        public event EmailSendingThreadExitEventHandler OnEmailSendingThreadExit;

        public event EmailSendingProgressChangedEventHandler OnEmailSendingProgressChangedExit;

        private SmtpClient _client;
        private ILogger _logger = LogManager.GetLogger(typeof(SmtpMailSender));
        public bool IsThreadRunning { get; private set; }
        public bool CancelThread { get; set; } = false;
        public string Username { get; set; }
        public string Pwd { get; set; }
        public int Port { get; set; } = 25;

        //public readonly Queue<MailMessage> _queueMail = new Queue<MailMessage>();
        private Thread _sendingThread;

        /// <summary>
        /// anon
        /// </summary>
        /// <param name="server"></param>
        /// <param name="port"></param>
        public SmtpMailSender(string server, int port)
        {
            Server = server;
            Port = port;
        }
        private void ConnectClient()
        {
            Log("Connecting SMTP...");
            _client = new SmtpClient
            {
                ServerCertificateValidationCallback = (s, c, h, e) => true,
            };
            _client.AuthenticationMechanisms.Remove("XOAUTH2");
            _client.Connect(Server, Port, false);
            if(!string.IsNullOrEmpty(Pwd))
                _client.Authenticate(Username, Pwd);
        }
        public void SetSmtpAccount(string userName, string pwd)
        {
            Username = userName;
            Pwd = pwd;
        }

        //public SmtpMailSender(string emailAccount, string pwd, string server, int port)
        //{
        //    EmailAccount = emailAccount;
        //    Server = server;
        //    _client = new SmtpClient
        //    {
        //        UseDefaultCredentials = false,
        //        Credentials = new NetworkCredential(EmailAccount, pwd),
        //        Port = port,
        //        Host = Server,
        //        DeliveryMethod = SmtpDeliveryMethod.Network,
        //        Timeout = Timeout,
        //        EnableSsl = false //seems faster with this shit off
        //    };
        //    // port 587, 25..
        //    // 25 seems to do the trick
        //}

        //public string EmailAccount { get; set; }
        public string Server { get; }
        public int SleepInterval { get; set; } = 1500;
        public int Timeout { get; set; } = 30000;
        public ConcurrentQueue<MimeMessage> MailQueue { get; set; } = new ConcurrentQueue<MimeMessage>();

        public void EnqueueEmail(MimeMessage mail)
        {
            MailQueue.Enqueue(mail);
        }

        public void EnqueueEmail(IEnumerable<MimeMessage> mailCollection)
        {
            if (MailQueue.Count > 0)
                foreach (var mail in mailCollection)
                    MailQueue.Enqueue(mail);
            else
                MailQueue = new ConcurrentQueue<MimeMessage>(mailCollection);
        }

        public void StartSending()
        {
            if (IsThreadRunning) return; //already running
            _sendingThread = new Thread(SendingThread) { IsBackground = true };

            try
            {
                _sendingThread.Start();
                IsThreadRunning = true;
            }
            catch (Exception ex)
            {
                Log("Start thread exception: " + ex.Message ?? string.Empty);
                //whatever, retry next time. this is safemeasure, dont think we can reach this.
            }
        }

        protected virtual void RaiseOnSendingThreadExit(EmailSendingThreadEventArgs e)
        {
            OnEmailSendingThreadExit?.Invoke(this, e);
        }

        protected virtual void RaiseOnSendingProgressChanged(int sent)
        {
            OnEmailSendingProgressChangedExit?.Invoke(this, new EmailSendingProgressChangedArgs(sent));
        }

        private void SendingThread()
        {
            int sleep = 0;
            int emailCount = 0;
            int retries = 0;
            bool unrecoverableEx = false;
            bool reconnectRequired = false;
            string exMessage = string.Empty;
            try
            {
                ConnectClient();
                //throw new AggregateException("test");
                while (!CancelThread)
                {
                    Thread.Sleep(sleep);
                    if (reconnectRequired)
                    {
                        ConnectClient();
                        reconnectRequired = false;
                    }

                    if (!MailQueue.TryDequeue(out MimeMessage anEmail))
                    {
                        Log("All emails processed -> stop thread");
                        return;
                    }
                    string address = anEmail.To.First().ToString();

                    //TODO: use Folly
                    try //retry
                    {
                        //_client.Timeout = 1;
                        Log(string.Format("Start sending email to {0}, total recipients: {1}", address,
                            anEmail.To.Count));
                        //throw new SmtpCommandException(SmtpErrorCode.MessageNotAccepted, SmtpStatusCode.AuthenticationChallenge, "Connection timed out");
                        _client.Send(anEmail);
                        Log("Sent sucessfully.");
                        RaiseOnSendingProgressChanged(emailCount);
                        sleep = 0;
                        emailCount++;
                    }
                    catch (ServiceNotConnectedException ex) //random drop of connection
                    {
                        Log($"Dropped connection -> sleep  for {SleepInterval} then retry.");
                        MailQueue.Enqueue(anEmail);
                        sleep = SleepInterval;
                        reconnectRequired = true;
                    }
                    catch (SmtpCommandException ex) when (ex.Message.Contains("Connection timed out"))
                    {
                        Log($"Timed out -> sleep  for {SleepInterval} then retry.");
                        MailQueue.Enqueue(anEmail);
                        sleep = SleepInterval;
                        reconnectRequired = true;
                    }
                }
            }
            catch (SocketException ex) when (ex.Message.Contains("No such host is known"))
            {
                exMessage = ex.Message;
                Log(exMessage);
                unrecoverableEx = true;
            }
            catch (SocketException ex) when (ex.Message.Contains("No connection could be made because the target machine actively refused it"))
            {
                exMessage = ex.Message;
                Log(exMessage);
                unrecoverableEx = true;
            }
            catch (AuthenticationException ex) when (ex.Message.Contains("AuthenticationInvalidCredentials"))
            {
                exMessage = ex.Message;
                Log(exMessage);
                unrecoverableEx = true;
            }
            catch (Exception ex)
            {
                unrecoverableEx = true;
                exMessage = "Unhandled exception in thread.";
                Log(ex.GetType().ToString());
                Log(exMessage);
                Log(ex.Message ?? string.Empty);
                Log(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Log(" Inner ex: " + ex.InnerException.Message ?? string.Empty);
                    Log(" Inner ex stacktrace: " + ex.InnerException.StackTrace);
                }

                return;
            }
            finally
            {
                _client.Disconnect(true);
                IsThreadRunning = false;
                RaiseOnSendingThreadExit(new EmailSendingThreadEventArgs(emailCount, retries, unrecoverableEx, exMessage));
                Log("Thread stopped.");
                //GC.Collect();
            }
        }
        private static string LogPath => string.Format(@"{0}\{1}", Program.ExeDir, "log.txt");
        private object _lock = new object();
        private void Log(string log)
        {
            //not really need to lock here, since only 1 thread execution
            //just in case... and in no ways seem to hurt anything :/
            lock (_lock)
            {
                //File.AppendAllLines(LogPath, new List<string> { FormatLog(log) }, Encoding.UTF8);
                _logger.Log(log);
            }
        }
        private static string FormatLog(string log)
        {
            return string.Format("{0:G} - {1}", DateTime.Now, log);
        }
    }

    public class EmailSendingThreadEventArgs : EventArgs
    {
        public int TotalSent { get; private set; }
        public int TotalRetries { get; private set; }
        public bool StopOnUnrecoverableException { get; private set; }
        public string ExceptionMessage { get; private set; } = string.Empty;

        public EmailSendingThreadEventArgs(int sent, int retries, bool unrecoverableException, string exMess)
        {
            TotalSent = sent;
            TotalRetries = retries;
            StopOnUnrecoverableException = unrecoverableException;
            ExceptionMessage = exMess;
        }
    }

    public class EmailSendingProgressChangedArgs : EventArgs
    {
        public int Sent { get; private set; }

        public EmailSendingProgressChangedArgs(int sent)
        {
            Sent = sent;
        }
    }
}
