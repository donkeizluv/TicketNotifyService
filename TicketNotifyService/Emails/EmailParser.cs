using MimeKit;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using TicketNotifyService.Config;
using TicketNotifyService.Log;
using TicketNotifyService.SQL;
using TicketNotifyService.Tickets;

namespace TicketNotifyService.Emails
{
    /// <summary>
    /// this class is intented to use in small scope
    /// </summary>
    public class EmailParser : IDisposable
    {
        public static ServiceConfig Config;

        private static void Log(string log) => _logger.Log(log);
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(EmailParser));

        private SqlWrapper _wrapper;
        private Ticket _ticket;

        public EmailParser(SqlWrapper sql, Ticket ticket)
        {
            _wrapper = sql;
            _ticket = ticket;
        }

        public MimeMessage ToEmailMessage()
        {
            if (Config == null) throw new InvalidProgramException();

            var stringWriter = new StringWriter();
            var email = new MimeMessage();
            //add addresses
            email.From.Add(Config.FromAddress);
            var additionalAddress = ParseFieldsToAddress(_ticket.Fields);
            email.To.AddRange(new []{ Config.HelpdeskAddress, _ticket.From });
            email.Cc.AddRange(additionalAddress);
            email.Subject = SubjectName(_ticket);

            //email content
            using (var writer = new HtmlTextWriter(stringWriter))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Html); //start HTML
                writer.RenderBeginTag(HtmlTextWriterTag.Head);

                //writer.RenderBeginTag(HtmlTextWriterTag.Style);
                //writer.Write("")
                //writer.RenderEndTag(); //end STYLE

                //style goes here
                writer.RenderEndTag(); //end HEAD
                writer.RenderBeginTag(HtmlTextWriterTag.Body);
                //content goes here

                //form type
                writer.RenderBeginTag(HtmlTextWriterTag.P);
                writer.RenderBeginTag(HtmlTextWriterTag.H3);
                writer.Write(_ticket.FormType);
                writer.RenderEndTag();
                writer.RenderEndTag();


                //created on
                writer.RenderBeginTag(HtmlTextWriterTag.P);
                writer.RenderBeginTag(HtmlTextWriterTag.H4);
                writer.Write(_ticket.Created.ToString());
                writer.RenderEndTag();
                writer.RenderEndTag();


                //ticket body
                writer.RenderBeginTag(HtmlTextWriterTag.P);
                writer.Write(_ticket.Body); //user description of the problem
                writer.RenderEndTag();

                //write fields
                HandleContainerFields(writer, email, out var attParts);

                writer.RenderEndTag(); //end BODY
                writer.RenderEndTag(); //end HTML

                var bodyBuilder = new BodyBuilder()
                {
                    HtmlBody = stringWriter.ToString()
                };
                email.Body = bodyBuilder.ToMessageBody();
            }
            return email;
        }

        private static string SubjectName(Ticket ticket)
        {
            return $"{ticket.FormType} - {ticket.From.ToString().Split('@').First()}";
        }

        private static void WrapTag(HtmlTextWriter writer, string value, HtmlTextWriterTag tag, string style = "")
        {
            if(!string.IsNullOrEmpty(style))
                writer.AddAttribute(HtmlTextWriterAttribute.Style, style);
            writer.RenderBeginTag(tag);
            writer.Write(value);
            writer.RenderEndTag();
        }

        private static string TdStyle = "border: 1px solid black;";
        private void HandleContainerFields(HtmlTextWriter writer, MimeMessage email, out List<MimeParser> attachmentParts)
        {
            attachmentParts = new List<MimeParser>();

            writer.AddAttribute(HtmlTextWriterAttribute.Style, "border-collapse: collapse;");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "5");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            foreach (var con in _ticket.Fields)
            {
                //if (con.IsAttachment || con.IsEmail) continue;

                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                WrapTag(writer, con.FieldLabel, HtmlTextWriterTag.Td, TdStyle);
                string value = con.FieldValue;
                if (con.IsAttachment)
                {
                    var pairs = JsonToKeyPair(value);
                    //add attachments
                    //Log($"Cant get Filename for FileId [{value}] -> Skip this att");


                }
                if (con.IsChoices)
                {
                    value = KeyPairsToString(JsonToKeyPair(value));
                }

                WrapTag(writer, value, HtmlTextWriterTag.Td, TdStyle);
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
        }

        private static string KeyPairsToString(List<KeyValuePair<string, string>> pairs)
        {
            var listSep = " | ";
            var builder = new StringBuilder();
            pairs.ForEach(p => builder.Append(p.Value).Append(listSep));
            return builder.ToString().Remove(builder.ToString().Length - 1 - listSep.Length, listSep.Length);
        }

        private static List<KeyValuePair<string, string>> JsonToKeyPair(string json)
        {
            var list = new List<KeyValuePair<string, string>>();
            var array = JArray.Parse(json);
            foreach (JObject obj in array.Children<JObject>())
            {
                foreach (JProperty singleProp in obj.Properties())
                {
                    list.Add(new KeyValuePair<string, string>(singleProp.Name, singleProp.Value.ToString()));
                }
            }
            return list;
        }

        private static string SearchFile(string root, string searchString)
        {
            try
            {
                return Directory.GetFiles(root, searchString, SearchOption.AllDirectories).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Log("Search file failed");
                Log(ex.Message);
                return string.Empty;
            }
        }

        private static MimePart MakeAttachment(string fullFilename)
        {
            if (string.IsNullOrEmpty(fullFilename))
                throw new ArgumentException("attachment file name is null or empty");
            var attachment = new MimePart("text", "plain")
            {
                ContentObject = new ContentObject(File.OpenRead(fullFilename), ContentEncoding.Default),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = Path.GetFileName(fullFilename)
            };
            return attachment;
        }

        private static TextPart MakeBodyPart(string body)
        {
            var part = new TextPart("plain")
            {
                Text = body
            };
            return part;
        }

        private static List<InternetAddress> ParseFieldsToAddress(List<FieldContainer> containers)
        {
            var emails = new List<InternetAddress>();
            foreach (var container in containers)
            {
                if (string.IsNullOrEmpty(container.FieldValue)) continue;
                if(container.IsEmail)
                {
                    if(InternetAddress.TryParse(container.FieldValue, out var email))
                    {
                        emails.Add(email);
                        continue;
                    }
                    Log($"Failed to parsed to email address: {container.FieldValue}");
                }
            }
            return emails;
        }

        public void Dispose()
        {
        }
    }
}
