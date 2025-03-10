using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMHReportingClientEmailService.Models
{
    public class EmailLogModel
    {
        public Int64 Id { get; set; }
        public string Event { get; set; }
        public string EmailSubj { get; set; }
        public string EmailBody { get; set; }
        public int eFlag { get; set; }
        public string DateCreated { get; set; }
        public string DateSent { get; set; }
    }
}
