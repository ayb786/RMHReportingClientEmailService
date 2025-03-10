using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMHReportingClientEmailService.Models
{
    public class LogModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string DateCreated { get; set; }
        public string Event { get; set; }
    }
}
