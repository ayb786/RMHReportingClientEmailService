﻿using Microsoft.Win32;
using RMHReportingClientEmailService.Models;
using RMHReportingClientEmailService.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace RMHReportingClientEmailService
{
    public class SendEmail
    {
        EmailRepository repo = null;
        //List<SyncStatusModel> sync = new List<SyncStatusModel>();
        SftpModel sftp = new SftpModel();
        ClientTypeModel clientType = new ClientTypeModel();
        int sftpErrorCount = 0;
        IDictionary<int, string> lstServices = null;
        string log_path = @"C:\ProgramData\RMH_API_Client_Logs\RMHReportingClientEmailService\";

        public SendEmail()
        {
            try
            {

                //This will check WOW6432Node folder by default in 64bit window
                RegistryKey old_key_path = Registry.LocalMachine.OpenSubKey(@"Software\RMHReportingClientSettings", true);


                //This will check Actual SOFTWARE folder for both 32bit & 64bit window
                Microsoft.Win32.RegistryKey baseReg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                Microsoft.Win32.RegistryKey key = baseReg.OpenSubKey(@"SOFTWARE\RMHReportingClientSettings", true);


                if (key != null && key.ValueCount > 0)
                {
                    DatabaseSettings.SqlServerName = key.GetValue("SqlServerName").ToString();
                    DatabaseSettings.Username = key.GetValue("Username").ToString();
                    DatabaseSettings.Password = key.GetValue("Password").ToString();
                    DatabaseSettings.Database = key.GetValue("Database").ToString();
                    key.Close();

                }
                else if (old_key_path != null && old_key_path.ValueCount > 0)
                {
                    DatabaseSettings.SqlServerName = old_key_path.GetValue("SqlServerName").ToString();
                    DatabaseSettings.Username = old_key_path.GetValue("Username").ToString();
                    DatabaseSettings.Password = old_key_path.GetValue("Password").ToString();
                    DatabaseSettings.Database = old_key_path.GetValue("Database").ToString();
                    old_key_path.Close();
                }
                else
                {
                    DatabaseSettings.SqlServerName = "";
                    DatabaseSettings.Username = "";
                    DatabaseSettings.Password = "";
                    DatabaseSettings.Database = "";
                }

                if (!string.IsNullOrEmpty(DatabaseSettings.Database))
                {
                    repo = new EmailRepository();
                    sftp = repo.GetSftpCredentials();

                    //var localDirectory = Directory.GetCurrentDirectory().ToString() + @"\Log";
                    var localDirectory = log_path;
                    if (!Directory.Exists(localDirectory))
                    {
                        Directory.CreateDirectory(localDirectory);
                    }

                    lstServices = new Dictionary<int, string>();
                    lstServices.Add(1, "ReportingClientEmailService");
                }
            }
            catch (Exception ex)
            {
                ServiceLog("RMHReportingClientEmailService", ex.Message);
            }
        }


        public void ReloadGlobalSettings()
        {
            try
            {

                //This will check WOW6432Node folder by default in 64bit window
                RegistryKey old_key_path = Registry.LocalMachine.OpenSubKey(@"Software\RMHReportingClientSettings", true);


                //This will check Actual SOFTWARE folder for both 32bit & 64bit window
                Microsoft.Win32.RegistryKey baseReg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                Microsoft.Win32.RegistryKey key = baseReg.OpenSubKey(@"SOFTWARE\RMHReportingClientSettings", true);


                if (key != null && key.ValueCount > 0)
                {
                    DatabaseSettings.SqlServerName = key.GetValue("SqlServerName").ToString();
                    DatabaseSettings.Username = key.GetValue("Username").ToString();
                    DatabaseSettings.Password = key.GetValue("Password").ToString();
                    DatabaseSettings.Database = key.GetValue("Database").ToString();
                    key.Close();

                }
                else if (old_key_path != null && old_key_path.ValueCount > 0)
                {
                    DatabaseSettings.SqlServerName = old_key_path.GetValue("SqlServerName").ToString();
                    DatabaseSettings.Username = old_key_path.GetValue("Username").ToString();
                    DatabaseSettings.Password = old_key_path.GetValue("Password").ToString();
                    DatabaseSettings.Database = old_key_path.GetValue("Database").ToString();
                    old_key_path.Close();
                }
                else
                {
                    DatabaseSettings.SqlServerName = "";
                    DatabaseSettings.Username = "";
                    DatabaseSettings.Password = "";
                    DatabaseSettings.Database = "";
                }


                if (!string.IsNullOrEmpty(DatabaseSettings.Database))
                {
                    clientType = repo.GetClientType();
                    sftp = repo.GetSftpCredentials();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Start()
        {
            try
            {
                var thread1 = new Thread(SendAutoEmail);
                //var thread2 = new Thread(GetNextSyncStatus);
                thread1.Start();
                //thread2.Start();
            }
            catch (Exception)
            {
            }
        }


        public void SendAutoEmail()
        {
            while (true)
            {
                try
                {

                    ReloadGlobalSettings();

                    //get smtp details
                    var email = repo.GetNotificationEmail();

                    if (email == null || string.IsNullOrEmpty(email.SmtpGateway) || string.IsNullOrEmpty(email.Port) || string.IsNullOrEmpty(email.SourceEmail) || string.IsNullOrEmpty(email.SourcePassword) || string.IsNullOrEmpty(email.TargetEmail1))
                    {
                        //repo.InsertServiceLog("Error", "Email configurations are missing ", "2"); //we will not save log in here, it can create lot of entries.
                        Thread.Sleep(2 * 60 * 1000); //2 min
                        continue;
                    }

                    //get stopped service for email
                    List<string> services = new List<string>();
                    foreach (var svc in lstServices)
                    {
                        //var sync = repo.GetNextSyncStatus(svc.Key);

                        //string lastRun = null;
                        //string nextRun = null;
                        //string curTime = null;

                        //var isEmailRequired = false;
                        //foreach (var item in sync) //list may contains multiple hour values.
                        //{
                        //    int hr24 = DateTime.Now.Hour == 0 ? 24 : DateTime.Now.Hour;
                        //    if (item.IntervalType == "HOURS")
                        //    {
                        //        lastRun = Convert.ToDateTime(item.LastSyncTime).ToString("yyyy-MM-dd HH:mm");
                        //        nextRun = Convert.ToDateTime(item.NextSyncTime).ToString("yyyy-MM-dd HH:mm");
                        //        curTime = (hr24 == 24) ? "24:" + DateTime.Now.Minute.ToString().PadLeft(2, '0') : DateTime.Now.ToString("HH:mm");
                        //        isEmailRequired = (item.IntervalTime.ToString().PadLeft(2, '0') + ":00" == curTime && item.SyncStatus == 1) ? true : false;

                        //        DateTime dt1 = DateTime.Parse(lastRun);
                        //        DateTime dt2 = DateTime.Now;

                        //        var result = DateTime.Compare(dt1, dt2);
                        //        //Less than zero dt1 is earlier than dt2.
                        //        //Zero dt1 is the same as dt2.
                        //        //Greater than zero dt1 is later than dt2.

                        //        var hours = (dt2 - dt1).TotalHours;

                        //        isEmailRequired = (svc.Key == 1 && hours >= 485) ? true : (svc.Key == 2 && hours >= 125) ? true : (svc.Key == 3 && hours >= 1445) ? true : false;



                        //    }
                        //    else if (item.IntervalType == "MINS")
                        //    {
                        //        lastRun = Convert.ToDateTime(item.LastSyncTime).ToString("yyyy-MM-dd HH:mm");
                        //        nextRun = Convert.ToDateTime(item.NextSyncTime).ToString("yyyy-MM-dd HH:mm");
                        //        curTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                        //        isEmailRequired = (DateTime.Parse(nextRun, CultureInfo.InvariantCulture) <= DateTime.Parse(curTime, CultureInfo.InvariantCulture) && item.SyncStatus == 1) ? true : false;

                        //        DateTime dt1 = DateTime.Parse(lastRun);
                        //        DateTime dt2 = DateTime.Now;

                        //        var result = DateTime.Compare(dt1, dt2);
                        //        //Less than zero dt1 is earlier than dt2.
                        //        //Zero dt1 is the same as dt2.
                        //        //Greater than zero dt1 is later than dt2.


                        //        var hours = (dt1 - dt2).TotalHours;

                        //        isEmailRequired = (svc.Key == 1 && hours >= 485) ? true : (svc.Key == 2 && hours >= 125) ? true : (svc.Key == 3 && hours >= 1445) ? true : false;

                        //    }

                        //}

                        var isEmailRequired = false;
                        var reslt = repo.GetLastSuccessServiceRun(svc.Key);

                        if (reslt != null)
                        {
                            DateTime dt1 = DateTime.Parse(reslt.LastSyncTime.ToString());
                            DateTime dt2 = DateTime.Now;
                            //var minutes = (dt2 - dt1).Minutes;
                            var minutes = (int)dt2.Subtract(dt1).TotalMinutes;

                            isEmailRequired = (svc.Key == 1 && minutes >= 485) ? true : (svc.Key == 2 && minutes >= 125) ? true : (svc.Key == 3 && minutes >= 1445) ? true : false;
                        }


                        var svcStatus = CheckInstalledServicesStatus(@"RMH" + svc.Value);

                        if (!string.IsNullOrEmpty(svcStatus) && svcStatus == "stopped" && isEmailRequired == true)
                        {
                            //services.Add(svc);
                            repo.InsertServiceLog("Error", "" + svc.Value + " is stopped and not processing any tasks assigned", "0");
                        }
                    }

                    /*** Disabled This on 24-09-2024  ***/
                    ////get pending error logs for email
                    //var erroLogs = repo.GetPendingErrorLog();
                    //if (erroLogs != null && erroLogs.Count() > 0)
                    //{

                    //    var filteredLogList = repo.SkipDuplicateErrorLog(erroLogs, email.IgnoreNextErrorFor);

                    //    if (filteredLogList != null && filteredLogList.Count() > 0)
                    //    {
                    //        SendMail(filteredLogList, services, email);
                    //    }
                    //}




                    //T119: New PO email notifcation
                    //Get New PO Email Log
                    var lst = repo.GetEmailLog();
                    if (lst != null && lst.Count() > 0)
                    {
                        SendPOMail(lst, email);
                    }


                    int loop = repo.GetEmailLoopInterval();
                    loop = (loop < 2) ? 2 : loop;
                    Thread.Sleep(loop * 60 * 1000); //2 min
                }
                catch (Exception ex)
                {
                    Thread.Sleep(2 * 60 * 1000); //2 min
                    continue;
                }
            }
        }



        public bool SendPOMail(List<EmailLogModel> logList, EmailModel email)
        {
            try
            {

                foreach (EmailLogModel item in logList)
                {
                    MailMessage mail = new MailMessage();

                    //TO
                    //foreach (var item in mTo)
                    //{
                    //    mail.To.Add(mod.TargetEmail1);
                    //}
                    mail.To.Add(email.TargetEmail1);

                    ////CC
                    //foreach (var item in mCC)
                    //    {
                    //        mail.CC.Add(item.ToString());
                    //    }
                    if (!string.IsNullOrEmpty(email.TargetEmail2))
                    {
                        mail.CC.Add(email.TargetEmail2);
                    }

                    mail.From = new MailAddress(email.SourceEmail);


                    mail.Subject = item.EmailSubj;

                    //var client = repo.GetClientType();


                    mail.IsBodyHtml = true;
                    mail.Body = item.EmailBody + "<br />Time of submission: " + item.DateCreated;


                    SmtpClient mySmtpClient = new SmtpClient
                    {
                        Host = email.SmtpGateway,
                        Port = Convert.ToInt32(email.Port),
                        EnableSsl = true,
                        DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(email.SourceEmail, email.SourcePassword)
                    };

                    mySmtpClient.Send(mail);
                    mail.Dispose();

                    //repo.InsertServiceLog("Success", "New PO # "++" notification email sent.", "1");

                    //Update PO email log flag
                    repo.UpdateEmailLog(item);
                }

                return true;

            }
            catch (Exception ex)
            {
                var err = ex.Message;
                repo.InsertServiceLog("Error", "RMH reporting client notification email error ~ " + ex.Message, "2");
                //return false;
                throw ex;
            }
        }



        public string CheckInstalledServicesStatus(string service)
        {

            try
            {
                var svcs = ServiceController.GetServices();
                ServiceController ctl = ServiceController.GetServices().Where(x => x.ServiceName.ToLower() == service.ToLower()).FirstOrDefault();
                if (ctl != null)
                {
                    ServiceController sc = new ServiceController(service);
                    switch (sc.Status)
                    {
                        case ServiceControllerStatus.Running:
                            return "running";
                        case ServiceControllerStatus.Stopped:
                            return "stopped";
                        case ServiceControllerStatus.Paused:
                            return "paused";
                        case ServiceControllerStatus.StopPending:
                            return "stopping";
                        case ServiceControllerStatus.StartPending:
                            return "starting";
                        default:
                            return "status changing";
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }



        public void Stop()
        {
            try
            {

            }
            catch (Exception)
            {

            }
        }


        public void ServiceLog(string service, string msg)
        {
            try
            {
                DateTime now = DateTime.Now;
                var startDate = new DateTime(now.Year, now.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var date = startDate.ToString("MM-dd-yyyy");

                //string path = Directory.GetCurrentDirectory().ToString() + @"\Log\ServiceLog.txt";
                string path = log_path + @"\RMHReportingClientEmailService_" + date + ".txt";
                using (StreamWriter sr = new StreamWriter(path, true))
                {
                    sr.WriteLine(service + " : " + msg + " : " + DateTime.Now.ToString("MM/dd/yyyy HH:mm tt"));
                    sr.Close();
                }
            }
            catch (Exception)
            {

            }


        }

    }
}
