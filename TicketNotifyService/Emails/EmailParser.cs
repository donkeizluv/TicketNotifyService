using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using TicketNotifyService.Config;
using TicketNotifyService.Log;
using TicketNotifyService.Tickets;

namespace TicketNotifyService.Emails
{
    public static class EmailParser
    {
        public static ServiceConfig Config;

        private static void Log(string log) => _logger.Log(log);
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(EmailParser));

        public static MimeMessage ParseToEmail(Ticket ticket)
        {
            if (Config == null) throw new InvalidProgramException();

            var stringWriter = new StringWriter();
            var email = new MimeMessage();
            //add addresses
            email.From.Add(Config.FromAddress);
            var additionalAddress = ParseFieldsToAddress(ticket.Fields);
            email.To.Add(Config.HelpdeskAddress);
            email.Cc.AddRange(additionalAddress);

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
                writer.Write(ticket.FormType);
                writer.RenderEndTag();
                writer.RenderEndTag();


                //created on
                writer.RenderBeginTag(HtmlTextWriterTag.P);
                writer.RenderBeginTag(HtmlTextWriterTag.H4);
                writer.Write(ticket.Created.ToString());
                writer.RenderEndTag();
                writer.RenderEndTag();


                //ticket body
                writer.RenderBeginTag(HtmlTextWriterTag.P);
                writer.Write(ticket.Body); //user description of the problem
                writer.RenderEndTag();

                //write fields
                WriteFieldsTable(ticket.Fields, writer);

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
        private static void WrapTag(HtmlTextWriter writer, string value, HtmlTextWriterTag tag, string style = "")
        {
            if(!string.IsNullOrEmpty(style))
                writer.AddAttribute(HtmlTextWriterAttribute.Style, style);
            writer.RenderBeginTag(tag);
            writer.Write(value);
            writer.RenderEndTag();
        }
        private static string TdStyle = "border: 1px solid black;";
        private static void WriteFieldsTable(List<FieldContainer> containers, HtmlTextWriter writer)
        {
            string CleanJson(string value)
            {
                return value.Replace("\"", string.Empty).Replace("{", string.Empty).Replace("}", string.Empty).Split(':').First();
            }
            writer.AddAttribute(HtmlTextWriterAttribute.Style, "border-collapse: collapse;");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "5");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            foreach (var con in containers)
            {
                if (con.IsAttachment || con.IsEmail) continue;

                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                WrapTag(writer, con.FieldLabel, HtmlTextWriterTag.Td, TdStyle);
                WrapTag(writer, con.IsChoices ? CleanJson(con.FieldValue) : con.FieldValue, HtmlTextWriterTag.Td, TdStyle);
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
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
    }
}
