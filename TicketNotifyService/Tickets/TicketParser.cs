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

        public static Ticket ParseToTicket(List<IDictionary<string, object>> detailRows)
        {
            var ticket = new Ticket()
            {
                Fields = new List<FieldContainer>()
            };

            foreach (var row in detailRows)
            {
                if (ticket.TicketId == null) //if not set
                {
                    var value = row[Ticket.TicketIdColumnName];
                    if (value == null)
                        throw new InvalidDataException("TicketId is null");
                    if (!int.TryParse(value.ToString(), out var intValue))
                    {
                        throw new InvalidCastException("Fail to parse TicketId");
                    }
                    ticket.TicketId = intValue;
                }
                if (ticket.Created == null)
                {
                    var value = row[Ticket.CreatedColumnName];

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
                if (ticket.Body == null)
                {
                    var value = row[Ticket.TicketBodyColumnName];
                    //acceptable to NULL
                    if (value != null)
                    {
                        ticket.Body = value.ToString();
                    }
                    else
                        ticket.Body = string.Empty;
                }
                if (ticket.FormType == null)
                {
                    var value = row[Ticket.FormTypeColumnName];
                    if (value == null)
                    {
                        throw new InvalidDataException("FormType is null");
                    }
                    ticket.FormType = value.ToString();
                }
                if (ticket.From == null)
                {
                    var value = row[Ticket.FromColumnName].ToString();
                    if (!InternetAddress.TryParse(value, out var address))
                    {
                        throw new InvalidCastException("Fail to parse From email address");
                    }
                    ticket.From = address;
                }
                var varNameValue = row[FieldContainer.FieldVarColumnName];
                var labelValue = row[FieldContainer.FieldLabelColumnName];
                var fieldValue = row[FieldContainer.FieldValueColumnName];

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
