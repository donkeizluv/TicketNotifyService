using MimeKit;
using System;
using System.Collections.Generic;

namespace TicketNotifyService.Tickets
{
    public class Ticket
    {
        public int? TicketId { get; set; }
        public DateTime? Created { get; set; }
        public MailboxAddress From { get; set; }
        //public InternetAddressList To { get; set; }
        public string FormType { get; set; }
        public string Body { get; set; }
        public string Subject { get; set; }
        public List<FieldContainer> Fields { get; set; }

        public Ticket()
        {

        }
    }
}
