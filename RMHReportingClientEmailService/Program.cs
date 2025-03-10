using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace RMHReportingClientEmailService
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var service1 = HostFactory.Run(x =>
                {
                    x.Service<SendEmail>(s =>
                    {
                        s.ConstructUsing(demo => new SendEmail());
                        s.WhenStarted(demo => demo.Start());
                        s.WhenStopped(demo => demo.Stop());
                    });

                    x.RunAsLocalSystem();
                    x.StartAutomaticallyDelayed();
                    x.SetServiceName("RMHReportingClientEmailService");
                    x.SetDisplayName("RMH Reporting Client Email Service");
                    x.SetDescription("This is a windows services use to send rmh reporting client email notifications.");
                });

                int service1Value = (int)Convert.ChangeType(service1, service1.GetTypeCode());
                Environment.ExitCode = service1Value;
            }
            catch (Exception ex)
            {

            }
        }
    }
}
