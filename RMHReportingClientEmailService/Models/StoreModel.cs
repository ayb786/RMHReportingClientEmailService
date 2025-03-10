using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMHReportingClientEmailService.Models
{
    public class StoreModel
    {
        public string StoreId { get; set; }
        public string Location { get; set; }
        public string StoreRegion { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }
    }
}
