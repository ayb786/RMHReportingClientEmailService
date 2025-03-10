using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMHReportingClientEmailService.Models
{
   public class EmailModel
    {
        public string SmtpGateway { get; set; }
        public string Port { get; set; }
        public string EnableSsl { get; set; }
        public string SourceEmail { get; set; }
        public string SourcePassword { get; set; }
        public string TargetEmail1 { get; set; }
        public string TargetEmail2 { get; set; }       //CC email (optional)
        public int IgnoreNextErrorFor { get; set; }
    }
}
