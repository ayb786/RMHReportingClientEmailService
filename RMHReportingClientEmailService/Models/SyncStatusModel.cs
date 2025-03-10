using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMHReportingClientEmailService.Models
{
    public class SyncStatusModel
    {
        public string ScheduleType { get; set; }
        public string IntervalType { get; set; }
        public int IntervalTime { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public DateTime? NextSyncTime { get; set; }
        public int SyncStatus { get; set; }
    }
}
