using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace 控制台程序获取数据
{
    public static class DAL
    {
        //static string constr= "data source=(local); initial catalog=Idata; Integrated Security =SSPI";
        static readonly string constr = ConfigurationManager.ConnectionStrings["Idata"].ToString();

        public static bool ExecProcedureWithInputParameters(string procedurename,string paramsname, string paramsval)
        {
            using (SqlConnection con = new SqlConnection(constr))
            {
                if (con.State != ConnectionState.Open)
                {
                    con.Open();
                }
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandTimeout = 10;
                    cmd.Connection = con;
                    cmd.CommandText =procedurename;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue(paramsname, paramsval);
                    try
                    {
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }

                }
            }
            
        }

        public static DataSet GetAllItemSource(string sqltext, CommandType cmdtype)
        {
            try
            {
                DataSet ds = new DataSet();
                using (SqlConnection con = new SqlConnection(constr))
                {
                    if (con.State != ConnectionState.Open)
                    {
                        con.Open();
                    }
                    using (SqlCommand cmd = new SqlCommand())
                    {   
                        cmd.Connection = con;
                        cmd.CommandText = sqltext;
                        cmd.CommandType = cmdtype;
                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                        sda.Fill(ds);
                        return ds;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }
           
        }

        public static DataSet GetInfoPageWaitingTopN(string sqltext, CommandType cmdtype,int topCnt)
        {
            try
            {
                DataSet ds = new DataSet();
                using (SqlConnection con = new SqlConnection(constr))
                {
                    if (con.State != ConnectionState.Open)
                    {
                        con.Open();
                    }
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = sqltext;
                        cmd.CommandType = cmdtype;
                        cmd.Parameters.AddWithValue("topCnt", topCnt);
                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                        sda.Fill(ds);
                        return ds;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }

        }

        public static void LoadDataTableToDBModelTable(DataTable dt,string modeltable)
        {
            if (dt.Rows.Count > 0)
            {
                using (SqlBulkCopy bkc = new SqlBulkCopy(constr))
                {
                    bkc.DestinationTableName = modeltable;
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        bkc.ColumnMappings.Add(dt.Columns[i].ColumnName, dt.Columns[i].ColumnName);
                    }
                    bkc.WriteToServer(dt);
                }
            }
       
        }

        public static bool ExecProcedureNonParamerters(string procedurename)
        {
            using (SqlConnection con = new SqlConnection(constr))
            {
                if (con.State != ConnectionState.Open)
                {
                    con.Open();
                }
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = con;
                    cmd.CommandText = procedurename;
                    cmd.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                    catch {
                        return false;
                    }
                }
            }
        }

        public static bool IfExistsHistoryUrl(string url)
        {
            bool _result = false;

            using (SqlConnection con = new SqlConnection(constr))
            {
                if (con.State != ConnectionState.Open)
                {
                    con.Open();
                }
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = con;
                    cmd.CommandText = "pr_if_url_exsits";
                    cmd.CommandType = CommandType.StoredProcedure;

                    SqlParameter sp1 = new SqlParameter("@url", SqlDbType.NVarChar, -1);
                    sp1.Direction = ParameterDirection.Input;
                    sp1.Value = url;

                    SqlParameter sp2 = new SqlParameter("@return", SqlDbType.Bit);
                    sp2.Direction = ParameterDirection.ReturnValue;


                    cmd.Parameters.Add(sp1);
                    cmd.Parameters.Add(sp2);
                    cmd.ExecuteNonQuery();
                    Console.WriteLine(url+": "+sp2.Value.ToString());
                    if (int.Parse(sp2.Value.ToString()) == 1)
                    {
                        _result = true;
                    }
                
                }
            }

            return _result;

        }

        public static int WriteLog(int classId,int sourceId,int seqNo, string keyword, int pageNo,bool isRegMatch, bool isException, string responseMsg,string regPattern,string gatherBT,string gatherET,int gatherRows)
        {
            keyword = seqNo == 0 ? keyword : seqNo.ToString() + "-" + keyword;
            string _sql = @" insert into t_gather_list_logs(class_id,source_id,keyword,page_no,is_reg_match,is_exception,response,reg_pattern,gather_bt,gather_et,gather_rows)  values ("
                + classId.ToString()+ ","
                + sourceId.ToString()+","
                + "'" +keyword+ "',"
               + pageNo.ToString() + ","
                + "'" + isRegMatch + "',"
                + "'" + isException + "',"
                + "'" + responseMsg.Replace("'","''") + "',"
                + "'" + regPattern.Replace("'", "''") + "',"
                + "'" + gatherBT + "',"
                + "'" + gatherET + "',"
                + gatherRows.ToString()
                + "); ";
            return ExcuteNonQuery(_sql);
        }


        public static int ExcuteNonQuery(string strCmd)
        {
            using (SqlConnection conn = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(strCmd, conn))
                {
                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }
                    cmd.CommandText = strCmd;
                    cmd.CommandType = CommandType.Text;
                    return cmd.ExecuteNonQuery();
                }
               
            }
        }






    }
}
