using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketNotifyService.Tickets
{
    public class FieldContainer
    {
        private static List<string> EmailVarNameList = new List<string>() {"direct_sup1", "direct_sup2", "bds_email" }; //configurable?
        private static List<string> AttachmentVarNameList = new List<string>() { "pics" };
        private static List<string> ChoicesVarNameList = new List<string>() { "account_type" };

        public static readonly string FieldVarColumnName = "FieldVarName";
        public static readonly string FieldLabelColumnName = "FieldLabel";
        public static readonly string FieldValueColumnName = "FieldValue";


        public string FieldVarName { get; set; }
        public string FieldLabel { get; set; }
        public string FieldValue { get; set; }
        public bool IsEmail
        {
            get
            {
                return EmailVarNameList.Contains(FieldVarName);
            }
        }
        public bool IsAttachment
        {
            get
            {
                return AttachmentVarNameList.Contains(FieldVarName);
            }
        }
        public bool IsChoices
        {
            get
            {
                return ChoicesVarNameList.Contains(FieldVarName);
            }
        }


    }
}
