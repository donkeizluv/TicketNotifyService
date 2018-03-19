using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketNotifyService.Tickets
{
    public class FieldContainer
    {
        public static readonly List<string> EmailVarNameList = new List<string>() { "direct_sup1", "direct_sup2", "bds_email" }; //configurable?
        public static readonly List<string> AttachmentVarNameList = new List<string>() { "pics" };
        public static readonly List<string> JsonVarNameList = new List<string>() { "type" };
        public static readonly List<string> ChoicesVarNameList = new List<string>() { "account_type", "printer" };
        public static readonly List<string> ExcludeVarNameList = new List<string>() { "subject", "desc", "priority" };

        public static readonly string FieldVarColumnName = "FieldVarName";
        public static readonly string FieldLabelColumnName = "FieldLabel";
        public static readonly string FieldValueColumnName = "FieldValue";


        public string FieldVarName { get; set; }
        public string FieldLabel { get; set; }
        public string FieldValue { get; set; }

        //email parser will treat these with special care :)
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
        public bool IsJSONArray
        {
            get
            {
                return JsonVarNameList.Contains(FieldVarName);
            }
        }
        public bool IsChoices
        {
            get
            {
                return ChoicesVarNameList.Contains(FieldVarName);
            }
        }
        public bool IsExcludeInTable
        {
            get
            {
                return ExcludeVarNameList.Contains(FieldVarName);
            }
        }

    }
}
