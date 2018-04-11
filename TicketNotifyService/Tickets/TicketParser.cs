using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketNotifyService.Log;

namespace TicketNotifyService.Tickets
{
    public static class TicketParser
    {
        private static void Log(string log) => _logger.Log(log);
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(TicketParser));

        public static readonly string TopicIdColumnName = "TopicId";
        public static readonly string TicketIdColumnName = "TicketId";
        public static readonly string TicketNumberColumnName = "TicketNumber";
        public static readonly string CreatedColumnName = "Created";
        public static readonly string FromColumnName = "From";
        public static readonly string FormTypeColumnName = "FormType";
        public static readonly string TopicColumnName = "Topic";

        //public static readonly string TicketSubjectVarName = "subject";
        public static readonly string TicketBodyVarName = "subject";

        public static readonly string GeneralFormType = "New Ticket";


        public static Ticket ParseToTicket(List<IDictionary<string, object>> detailRows)
        {
            var ticket = new Ticket()
            {
                Fields = new List<FieldContainer>()
            };

            foreach (var row in detailRows)
            {
                if (ticket.TopicId == null) //if not set
                {
                    var value = row[TopicIdColumnName];
                    if (value == null)
                        throw new InvalidDataException("TopicId is null");
                    if (!int.TryParse(value.ToString(), out var intValue))
                    {
                        throw new InvalidDataException("Fail to parse TicketId to Int");
                    }
                    ticket.TopicId = intValue;
                }
                if (ticket.TicketId == null) //if not set
                {
                    var value = row[TicketIdColumnName];
                    if (value == null)
                        throw new InvalidDataException("TicketId is null");
                    if (!int.TryParse(value.ToString(), out var intValue))
                    {
                        throw new InvalidDataException("Fail to parse TicketId to Int");
                    }
                    ticket.TicketId = intValue;
                    _logger.Log($"ticket id:{intValue}");
                }
                if(string.IsNullOrEmpty(ticket.TicketNumber))
                {
                    ticket.TicketNumber = row[TicketNumberColumnName].ToString();
                }
                if (ticket.Created == null)
                {
                    var value = row[CreatedColumnName];

                    if (value != null && DateTime.TryParse(value.ToString(), out var dateValue))
                    {
                        ticket.Created = dateValue;
                    }
                    else
                    {
                        //acceptable to fail
                        Log($"Cant parse [{value ?? "NULL"}] to {nameof(ticket.Created)} -> Use Now DateTime");
                        ticket.Created = DateTime.Today;
                    }
                }
                if (ticket.Body == null) //in New Ticket form type -> desc var
                {
                    var value = row[FormTypeColumnName];
                    if(string.Compare(value.ToString(), GeneralFormType, true) == 0)
                    {
                        //MAGIC: WTF is this?
                        if (string.Compare(row[FieldContainer.FieldVarColumnName].ToString(), TicketBodyVarName, true) == 0)
                            ticket.Body = (row[FieldContainer.FieldValueColumnName] ?? string.Empty).ToString();
                    }
                }
                //if (ticket.Subject == null) //in New Ticket form type -> subject var
                //{
                //    var value = row[TopicColumnName];
                //    if (string.Compare(value.ToString(), GeneralFormType, true) == 0)
                //    {
                //        if (string.Compare(row[FieldContainer.FieldVarColumnName].ToString(), TicketSubjectVarName, true) == 0)
                //            ticket.Subject = (row[FieldContainer.FieldValueColumnName] ?? string.Empty).ToString();
                //    }
                //}
                if (ticket.FormType == null)
                {
                    var value = row[FormTypeColumnName];
                    if(string.Compare(value.ToString(), "New Ticket", true) != 0)
                        ticket.FormType = value.ToString();
                }
                if (ticket.From == null)
                {
                    var value = row[FromColumnName].ToString();
                    if (!MailboxAddress.TryParse(value, out var address))
                    {
                        throw new InvalidDataException("Fail to parse From email address");
                    }
                    ticket.From = address;
                }
                var varNameValue = row[FieldContainer.FieldVarColumnName];
                var labelValue = row[FieldContainer.FieldLabelColumnName];
                var fieldValue = row[FieldContainer.FieldValueColumnName];
                ticket.Subject = row[TopicColumnName].ToString();

                var container = new FieldContainer()
                {
                    FieldLabel = (labelValue ?? string.Empty).ToString(),
                    FieldVarName = (varNameValue ?? string.Empty).ToString(),
                    FieldValue = (fieldValue ?? string.Empty).ToString(),
                };
                ticket.Fields.Add(container);
            }
            return ticket;
        }
    }
}
