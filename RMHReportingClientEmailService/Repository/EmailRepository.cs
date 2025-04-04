using RMHReportingClientEmailService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace RMHReportingClientEmailService.Repository
{
    public class EmailRepository
    {

        string connString = "";
        public EmailRepository()
        {
            connString = @"Server=" + DatabaseSettings.SqlServerName + ";Database=" + DatabaseSettings.Database + ";User ID=" + DatabaseSettings.Username + ";Password=" + DatabaseSettings.Password + ";";
        }


        public EmailModel GetEmailConfigurations()
        {
            SqlConnection conn = new SqlConnection(connString);
            SqlCommand command = conn.CreateCommand();
            var StoreId = "";
            try
            {
                conn.Open();
                command.CommandText = @"SELECT * FROM ibp_EmailNotification_rmhreporting;";
                SqlDataReader dr = command.ExecuteReader();

                Encryptor ecpr = new Encryptor();

                EmailModel mod = new EmailModel();
                while (dr.Read())
                {
                    mod.SmtpGateway = dr["Gateway"].ToString();
                    mod.Port = dr["Port"].ToString();
                    mod.EnableSsl = dr["EnableSsl"].ToString();
                    mod.SourceEmail = dr["SourceEmail"].ToString();
                    mod.SourcePassword = !string.IsNullOrEmpty(dr["SourcePassword"].ToString()) ? ecpr.Decrypt(dr["SourcePassword"].ToString()) : "";
                    mod.TargetEmail1 = dr["TargetEmail1"].ToString();
                    mod.TargetEmail2 = dr["TargetEmail2"].ToString();
                    mod.IgnoreNextErrorFor = Convert.ToInt32(dr["IgnoreNextErrorFor"].ToString());

                }

                conn.Close();

                return mod;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        /***/
        public StoreModel GetStoreDetail()
        {
            SqlConnection conn = new SqlConnection(connString);
            SqlCommand command = conn.CreateCommand();
            try
            {
                conn.Open();
                command.CommandText = @"SELECT * FROM ibp_store_rmhreporting";
                SqlDataReader dr = command.ExecuteReader();

                StoreModel model = new StoreModel();
                while (dr.Read())
                {
                    model.StoreId = dr["StoreId"] == DBNull.Value ? "" : Convert.ToString(dr["StoreId"]);
                    model.ServerAddress = dr["ServerAddress"] == DBNull.Value ? "" : Convert.ToString(dr["ServerAddress"]);
                    model.Password = dr["Password"] == DBNull.Value ? "" : Convert.ToString(dr["Password"]);
                }

                conn.Close();

                return model;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        public bool InsertServiceLog(string status, string msg, int eflag)
        {
            SqlConnection conn = new SqlConnection(connString);
            SqlCommand command = conn.CreateCommand();
            int Result = 0;
            try
            {
                conn.Open();

                //eflag =2 for skip email

                // Step 1: Check if error already exist with in 10 min
                SqlDataAdapter cmd1 = new SqlDataAdapter("select event from ibp_log_rmhreporting where datecreated >= DATEADD(MINUTE, -10, GETDATE())", conn);
                DataTable dataTable = new DataTable();
                cmd1.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    bool exists = dataTable.AsEnumerable()
                        .Any(row => row["event"] != DBNull.Value &&
                                !string.IsNullOrEmpty(Convert.ToString(row["event"])) &&
                                row["event"].ToString().IndexOf(msg, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (exists)
                    {
                        conn.Close();
                        return false;
                    }
                }



                if (!string.IsNullOrEmpty(msg))
                {
                    msg = msg.Length <= 799 ? msg : msg.Substring(0, 798);
                    msg = msg.Replace("\'", "").Replace("\"", "").Replace(";", "");
                };

                command.CommandText = @"insert into ibp_log_rmhreporting 
                (status,datecreated,event,eflag) values 
                ('" + status + "',getdate(),'" + msg + "', '" + eflag + "');";

                Result = command.ExecuteNonQuery();

                conn.Close();

                if (Result > 0)
                {
                    return true;
                }
                return false;

            }
            catch (SqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        public int GetEmailLoopInterval()
        {
            SqlConnection conn = new SqlConnection(connString);
            SqlCommand command = conn.CreateCommand();
            var StoreId = "";
            conn.Open();
            try
            {
                command.CommandText = @"SELECT * FROM ibp_EmailNotification_rmhreporting;";
                SqlDataReader dr = command.ExecuteReader();


                while (dr.Read())
                {
                    var loop_interval = (DBNull.Value == dr["Loop"] || string.IsNullOrEmpty(dr["Loop"].ToString())) ? 2 : Convert.ToInt32(dr["Loop"].ToString());
                    conn.Close();

                    return loop_interval;
                }

                conn.Close();

                return 2;
            }
            catch (Exception ex)
            {
                conn.Open();
                //throw ex;
                return 2;
            }

        }


        public SyncStatusModel GetLastSuccessServiceRun(int svc_id)
        {
            SqlConnection conn = new SqlConnection(connString);
            SqlCommand command = conn.CreateCommand();
            try
            {

                List<SyncStatusModel> lst = new List<SyncStatusModel>();

                SyncStatusModel sync = null;

                SqlDataReader dr;
                using (command = new SqlCommand())
                {
                    if (svc_id == 1)
                    {
                        command.CommandText = @"
                    select top 1 id, status, datecreated, event, eflag from ibp_log_rmhreporting where event = 'Reporting client service completed.' order by datecreated desc ; ";
                    }
                    else
                    {
                        return null;
                    }



                    conn.Open();
                    command.CommandType = CommandType.Text;
                    command.Connection = conn;
                    using (dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            sync = new SyncStatusModel();

                            //sync.SyncStatus = Convert.ToInt32(dr["SyncStatus"].ToString());
                            //sync.ScheduleType = dr["stype"].ToString(); //service type

                            if (DBNull.Value != dr["datecreated"])
                            {
                                sync.LastSyncTime = Convert.ToDateTime(dr["datecreated"].ToString());
                            }
                            else
                            {
                                sync.LastSyncTime = null;
                            }



                        }
                    }
                }


                conn.Close();
                return sync;
            }
            catch (SqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        public List<LogModel> GetEmailLog()
        {
            SqlConnection conn = new SqlConnection(connString);
            SqlCommand command = conn.CreateCommand();
            try
            {
                conn.Open();
                command.CommandText = @"SELECT * FROM ibp_log_rmhreporting where status = 'ERROR' and eflag = 0 and (datecreated > DATEADD(day, -2, CONVERT (date, SYSDATETIME())));";

                SqlDataReader dr = command.ExecuteReader();

                List<LogModel> lst = new List<LogModel>();

                while (dr.Read())
                {
                    LogModel m = new LogModel();

                    m.Id = Convert.ToInt32(dr["Id"].ToString());
                    m.Status = dr["Status"].ToString();
                    m.DateCreated = Convert.ToDateTime(dr["DateCreated"].ToString()).ToString("MM/dd/yyyy HH:mm tt");
                    m.Event = dr["Event"].ToString();
                    if (!string.IsNullOrEmpty(m.Event))
                    {
                        m.Event = m.Event.Replace("'", "").Replace("'", @"\'").Replace("#", @"");
                    }

                    lst.Add(m);
                }

                conn.Close();
                return lst;
            }
            catch (SqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        //public List<EmailLogModel> GetEmailLog()
        //{
        //    SqlConnection conn = new SqlConnection(connString);
        //    SqlCommand command = conn.CreateCommand();
        //    try
        //    {
        //        conn.Open();

        //        //command.CommandText = @"DELETE FROM ibp_email_log_rmhreporting where eflag = 1 and (datecreated > DATEADD(day, -15, CONVERT (date, SYSDATETIME()))); ";


        //        command.CommandText = @"SELECT * FROM ibp_email_log_rmhreporting where eflag = 0 and (datecreated > DATEADD(day, -2, CONVERT (date, SYSDATETIME()))); ";
        //        SqlDataReader dr = command.ExecuteReader();

        //        List<EmailLogModel> lst = new List<EmailLogModel>();
        //        while (dr.Read())
        //        {
        //            EmailLogModel model = new EmailLogModel();
        //            model.Id = Convert.ToInt64(dr["id"].ToString());
        //            model.Event = dr["event"].ToString();
        //            model.EmailSubj = dr["EmailSubj"].ToString();
        //            model.EmailBody = dr["EmailBody"].ToString();
        //            model.DateCreated = Convert.ToDateTime(dr["DateCreated"].ToString()).ToString("MM/dd/yyyy HH:mm tt");

        //            lst.Add(model);
        //        }

        //        conn.Close();

        //        return lst;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //}



        public bool UpdateEmailLog(EmailLogModel log)
        {
            SqlConnection conn = new SqlConnection(connString);
            SqlCommand command = conn.CreateCommand();
            int Result = 0;
            try
            {

                conn.Open();

                //same name errors
                command.CommandText = @"UPDATE ibp_email_log_rmhreporting 
                    SET eflag = 1 where id = '" + log.Id + "' ;";
                Result = command.ExecuteNonQuery();

                conn.Close();

                if (Result > 0)
                {
                    return true;
                }
                return false;

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }



        public bool UpdateErrorEmailLog(LogModel mod)
        {
            SqlConnection conn = new SqlConnection(connString);
            SqlCommand command = conn.CreateCommand();
            int Result = 0;
            try
            {

                conn.Open();

                //same name errors
                command.CommandText = @"UPDATE ibp_log_rmhreporting 
                    SET eflag = 1 where id = '" + mod.Id + "' ;";
                Result = command.ExecuteNonQuery();

                conn.Close();

                if (Result > 0)
                {
                    return true;
                }
                return false;

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }



        /***/
        //public List<LogModel> GetPendingErrorLog()
        //{
        //    SqlConnection conn = new SqlConnection(connString);
        //    SqlCommand command = conn.CreateCommand();
        //    try
        //    {
        //        conn.Open();
        //        command.CommandText = @"SELECT * FROM ibp_log_rmhreporting where status = 'ERROR' and eflag = 0 and (datecreated > DATEADD(day, -2, CONVERT (date, SYSDATETIME())));";
        //        //command.CommandText = @"select a.* from ibp_log_rmhreporting a
        //        //left join ibp_emaillog_rmhreporting b on a.id  = b.id
        //        //where b.id is null";

        //        SqlDataReader dr = command.ExecuteReader();

        //        List<LogModel> lst = new List<LogModel>();

        //        while (dr.Read())
        //        {
        //            LogModel m = new LogModel();

        //            m.Id = Convert.ToInt32(dr["Id"].ToString());
        //            m.Status = dr["Status"].ToString();
        //            m.DateCreated = Convert.ToDateTime(dr["DateCreated"].ToString()).ToString("MM/dd/yyyy HH:mm tt");
        //            m.Event = dr["Event"].ToString();
        //            if (!string.IsNullOrEmpty(m.Event))
        //            {
        //                m.Event = m.Event.Replace("'", "").Replace("'", @"\'").Replace("#", @"");
        //            }

        //            lst.Add(m);
        //        }

        //        conn.Close();
        //        return lst;
        //    }
        //    catch (SqlException ex)
        //    {
        //        throw ex;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //}


        /***/
        //public List<LogModel> SkipDuplicateErrorLog(List<LogModel> lstLog, int ignoreNextErrorMins)
        //{
        //    SqlConnection conn = new SqlConnection(connString);
        //    SqlCommand command = conn.CreateCommand();
        //    int Result = 0;
        //    try
        //    {

        //        conn.Open();

        //        //Get lastupdated log time of event with email already sent
        //        foreach (var item in lstLog)
        //        {
        //            string lastEmailDate = "";
        //            SqlDataReader dr1;
        //            command.CommandText = @"select max(datecreated) as maxdate from  ibp_log_rmhreporting where event = '" + item.Event + "' and eflag = 1";

        //            using (dr1 = command.ExecuteReader())
        //            {
        //                while (dr1.Read())
        //                {
        //                    lastEmailDate = (DBNull.Value == dr1["maxdate"]) ? "" : Convert.ToDateTime(dr1["maxdate"].ToString()).ToString("yyyy-MM-dd HH:mm");
        //                }
        //            }


        //            if (lastEmailDate != "")
        //            {
        //                //same name errors
        //                command.CommandText = @"UPDATE ibp_log_rmhreporting 
        //                SET eflag = 2 where  event = '" + item.Event.Replace("'", "").Replace("'", @"\'").Replace("#", @"") + "' and eflag = 0 and DATEADD(minute, -" + ignoreNextErrorMins + ", '" + item.DateCreated + "') <= '" + lastEmailDate + "'  ;";
        //                Result = command.ExecuteNonQuery();
        //            }


        //        }

        //        command.CommandText = @"SELECT * FROM ibp_log_rmhreporting where status = 'ERROR' and eflag = 0";
        //        SqlDataReader dr = command.ExecuteReader();
        //        List<LogModel> lst = new List<LogModel>();
        //        while (dr.Read())
        //        {
        //            LogModel m = new LogModel();

        //            m.Id = Convert.ToInt32(dr["Id"].ToString());
        //            m.Status = dr["Status"].ToString();
        //            m.DateCreated = Convert.ToDateTime(dr["DateCreated"].ToString()).ToString("MM/dd/yyyy HH:mm tt");
        //            m.Event = dr["Event"].ToString();

        //            lst.Add(m);
        //        }

        //        conn.Close();
        //        return lst;

        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //}



        /***/
        //public ClientTypeModel GetClientType()
        //{
        //    SqlConnection conn = new SqlConnection(connString);
        //    SqlCommand command = conn.CreateCommand();
        //    var ClientType = "";
        //    try
        //    {
        //        conn.Open();
        //        command.CommandText = @"SELECT * FROM ibp_ClientType_rmhreporting";
        //        SqlDataReader dr = command.ExecuteReader();

        //        ClientTypeModel mod = new ClientTypeModel();
        //        while (dr.Read())
        //        {
        //            mod.Id = Convert.ToInt32(dr["Id"].ToString());
        //            mod.ClientType = dr["ClientType"].ToString();
        //        }

        //        conn.Close();

        //        return mod;
        //    }
        //    catch (SqlException ex)
        //    {
        //        throw ex;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //}


        /***/
        //public SftpModel GetSftpCredentials()
        //{
        //    SqlConnection conn = new SqlConnection(connString);
        //    SqlCommand command = conn.CreateCommand();
        //    var StoreId = "";
        //    try
        //    {
        //        conn.Open();
        //        command.CommandText = @"SELECT * FROM ibp_sftp_rmhreporting;";
        //        SqlDataReader dr = command.ExecuteReader();

        //        Encryptor ecpr = new Encryptor();

        //        SftpModel mod = new SftpModel();
        //        while (dr.Read())
        //        {
        //            mod.Host = ecpr.Decrypt(dr["Host"].ToString());
        //            mod.Port = ecpr.Decrypt(dr["Port"].ToString());
        //            mod.UserId = ecpr.Decrypt(dr["UserId"].ToString());
        //            mod.Password = ecpr.Decrypt(dr["Password"].ToString());
        //        }

        //        conn.Close();

        //        return mod;
        //    }
        //    catch (SqlException ex)
        //    {
        //        throw ex;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //}
    }
}
