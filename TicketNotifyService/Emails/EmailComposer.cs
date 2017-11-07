using MimeKit;
using Newtonsoft.Json;
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
    public class EmailComposer : IDisposable
    {
        public static ServiceConfig Config;

        private static void Log(string log) => _logger.Log(log);
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(EmailComposer));

        private SqlWrapper _wrapper;
        private Ticket _ticket;

        public EmailComposer(SqlWrapper sql, Ticket ticket)
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
            email.Subject = _ticket.Subject == string.Empty? SubjectName(_ticket) : _ticket.Subject;

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
                writer.RenderBeginTag(HtmlTextWriterTag.H4);
                writer.Write(_ticket.FormType);
                writer.RenderEndTag();
                writer.RenderEndTag();


                //created on
                writer.RenderBeginTag(HtmlTextWriterTag.P);
                writer.RenderBeginTag(HtmlTextWriterTag.H4);
                writer.Write(_ticket.Created.ToString());
                writer.RenderEndTag();
                writer.RenderEndTag();

                //From
                writer.RenderBeginTag(HtmlTextWriterTag.P);
                writer.RenderBeginTag(HtmlTextWriterTag.H4);
                writer.Write($"From: {_ticket.From.Address}");
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

                AddBody(email, attParts, stringWriter.ToString());
            }
            return email;
        }
        private static void AddBody(MimeMessage mail, List<MimePart> attParts, string body)
        {
            var multipart = new Multipart("mixed");
            foreach (var att in attParts)
            {
                multipart.Add(att);
            }
            multipart.Add(MakeBodyPart(body));
            mail.Body = multipart;
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
        private static string TableStyle = "border-collapse: collapse;";
        private void HandleContainerFields(HtmlTextWriter writer, MimeMessage email, out List<MimePart> attachmentParts)
        {
            attachmentParts = new List<MimePart>();
            writer.AddAttribute(HtmlTextWriterAttribute.Style, TableStyle);
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "5");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            foreach (var con in _ticket.Fields)
            {
                if (con.IsExcludeInTable) continue;

                string value = con.FieldValue;
                if (con.IsAttachment)
                {
                    //handle attachments
                    var pairs = JsonToKeyPairs(value);
                    //value = KeyPairsToString(pairs, true);
                    foreach (var pair in pairs)
                    {
                        var fileId = pair.Value;
                        var fileName = _wrapper.GetFilename(fileId);
                        if (string.IsNullOrEmpty(fileName))
                        {
                            Log($"Cant get Filename for FileId [{fileId}] -> Skip this att");
                            continue; //should not happen tho
                        }

                        var fullPath = SearchFile(Config.AttachmentRootFolder, fileName);
                        if (!string.IsNullOrEmpty(fullPath))
                        {
                            attachmentParts.Add(MakeAttachment(fullPath, pair.Key));
                        }
                        else
                        {
                            Log($"Cant find file {fileName} -> Skip this att");
                        }
                    }
                    //dont values print for attachments
                    continue;
                }
                if (con.IsChoices)
                {
                    //print formated choices
                    value = KeyPairsToString(JsonToKeyPairs(value));
                }
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                WrapTag(writer, con.FieldLabel, HtmlTextWriterTag.Td, TdStyle);
                WrapTag(writer, value, HtmlTextWriterTag.Td, TdStyle);
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
        }

        private static string KeyPairsToString(List<KeyValuePair<string, string>> pairs, bool takePropName = false)
        {
            if (pairs == null || pairs.Count < 1) return string.Empty;
            var listSep = " | ";
            var builder = new StringBuilder();
            if(takePropName)
                pairs.ForEach(p => builder.Append(p.Key).Append(listSep));
            else
                pairs.ForEach(p => builder.Append(p.Value).Append(listSep));

            return builder.ToString().Remove(builder.ToString().Length - listSep.Length, listSep.Length);
        }

        private static List<KeyValuePair<string, string>> JsonToKeyPairs(string json)
        {
            var list = new List<KeyValuePair<string, string>>();
            try
            {
                
                var array = JObject.Parse(json);
                foreach (JProperty prop in array.Properties())
                {
                    list.Add(new KeyValuePair<string, string>(prop.Name, prop.Value.ToString()));
                }
                return list;
            }
            catch (JsonReaderException ex)
            {
                Log($"Parse JSON object failed: {json}");
                return list;
            }

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

        private static MimePart MakeAttachment(string fullFilename, string fileName)
        {
            if (string.IsNullOrEmpty(fullFilename))
                throw new ArgumentException("attachment file name is null or empty");
            var attachment = new MimePart("text", "plain")
            {
                ContentObject = new ContentObject(File.OpenRead(fullFilename), ContentEncoding.Default),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = Path.GetFileName(fileName)
            };
            return attachment;
        }

        private static TextPart MakeBodyPart(string body)
        {
            var part = new TextPart("html")
            {
                Text = body
            };
            return part;
        }

        private static List<MailboxAddress> ParseFieldsToAddress(List<FieldContainer> containers)
        {
            var emails = new List<MailboxAddress>();
            foreach (var container in containers)
            {
                if (string.IsNullOrEmpty(container.FieldValue)) continue;
                if(container.IsEmail)
                {
                    if(MailboxAddress.TryParse(container.FieldValue, out var email))
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
