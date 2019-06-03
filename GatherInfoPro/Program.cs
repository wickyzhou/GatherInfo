﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Configuration;
using System.Web;

namespace 控制台程序获取数据
{
    class Program
    {
        static DataTable dtList = null; static DataTable dtInfoPage = null;
        static readonly string releaseConfig = ConfigurationManager.AppSettings["releaseConfig"];
        static readonly string runModel = ConfigurationManager.AppSettings["runModel"];
        static readonly int batchCount = int.Parse(ConfigurationManager.AppSettings["batchCount"]);


        //程序主函数：程序开始到程序结束
        static void Main(string[] args)
        {
            if (runModel == "debug")
            {
                ExecuteDebugByID();
                Console.ReadKey();
            }
            else
            {
                if (releaseConfig == "000")         //所有列表+所有信息
                {
                    ExecuteGatherItemSourceList();
                    ExecuteGatherSearchKeywordList();
                    ExecuteGatherInfoPageWaitingAll(batchCount);
                }
                else if (releaseConfig == "110")   //所有列表
                {
                    ExecuteGatherItemSourceList();
                    ExecuteGatherSearchKeywordList();
                }
                else if (releaseConfig == "111")    //网站栏目类型
                {
                    ExecuteGatherItemSourceList();

                }
                else if (releaseConfig == "112")    //关键字类型
                {
                    ExecuteGatherSearchKeywordList();
                }
                else if (releaseConfig == "120") //所有信息
                {
                    ExecuteGatherInfoPageWaitingAll(batchCount);
                }
                else
                {
                    Console.WriteLine("\n\t===  releaseConfig 参数的value值错误，请检查【配置文件】格式,输入注释的可选值  ===");
                    Console.ReadKey();
                }
            }

        }

        /// <summary>
        /// 调试采集列表和信息的总入口点
        /// </summary>
        private static void ExecuteDebugByID()
        {
            string gatherType = string.Empty;
            string queryString = OnGetValidInputGatherTypeAndString(out gatherType);
            DataSet ds = DAL.GetAllItemSource(queryString, CommandType.Text);
            if (gatherType == "1")
            {
                ListMaster(ds);
            }
            else
            {
                InfoPageMaster(ds);
            }
        }

        /// <summary>
        /// 采集所有的栏目列表采集源入口点
        /// </summary>
        /// <param name="runModel"></param>
        private static void ExecuteGatherItemSourceList()
        {
            string queryString = OnGetItemSourceListString();
            DataSet ds = DAL.GetAllItemSource(queryString, CommandType.Text);
            ListMaster(ds);
        }

        /// <summary>
        /// 采集所有的关键字列表采集源入口点
        /// </summary>
        /// <param name="runModel"></param>
        private static void ExecuteGatherSearchKeywordList()
        {
            string queryString = OnGetSearchKeywordListString();
            DataSet ds = DAL.GetAllItemSource(queryString, CommandType.Text);
            ListMaster(ds);
        }

        /// <summary>
        /// 采集所有的待采信息入口点
        /// </summary>
        /// <param name="batchCount"></param>
        private static void ExecuteGatherInfoPageWaitingAll(int batchCount)
        {
            DataSet ds = new DataSet("InfoPageWaiting");
            do
            {
                ds = DAL.GetInfoPageWaitingTopN("pr_gather_infopage_waiting", CommandType.StoredProcedure, batchCount);
                InfoPageMaster(ds);
            }
            while (ds.Tables[0].Rows.Count == batchCount);
        }

        /// <summary>
        /// 采集源循环采集
        /// </summary>
        /// <param name="ds"></param>
        private static void ListMaster(DataSet ds)
        {
            for (int i = 0; i < ds.Tables.Count; i++)
            {
                for (int j = 0; j < ds.Tables[i].Rows.Count; j++)
                {
                    #region 获取并初始化需要的通用行数据   先将object 转化为string,再用type.Parse  兼容性转化。 如果直接类型转化则必须和数据库类型一一对应
                    int sourceID = int.Parse(ds.Tables[i].Rows[j]["id"].ToString()); //采集源ID
                    int provinceID = int.Parse(ds.Tables[i].Rows[j]["province_id"].ToString()); //运营中心ID
                    int classID = int.Parse(ds.Tables[i].Rows[j]["class_id"].ToString()); //类别：列表或关键字或者招标公告或者...主要是为了和ID生成唯一的采集源
                    int listOpcID = int.Parse(ds.Tables[i].Rows[j]["list_opc_id"].ToString());    //链接页面对应OPC表的ID
                    int infoOpcID = int.Parse(ds.Tables[i].Rows[j]["infopage_opc_id"].ToString()); //信息页面对应OPC表的ID
                    string sourceUrl = ds.Tables[i].Rows[j]["source_url"].ToString();//采集源首页
                    string gatherUrl = ds.Tables[i].Rows[j]["gather_url"].ToString();//参数化、结构化的采集源
                    string listP1 = ds.Tables[i].Rows[j]["list_p1"].ToString() == "-" ? "" : ds.Tables[i].Rows[j]["list_p1"].ToString();  //列表中变化参数1，同$pageno，
                    string listP2 = ds.Tables[i].Rows[j]["list_p2"].ToString() == "-" ? "" : ds.Tables[i].Rows[j]["list_p2"].ToString();  //列表中变化参数2，同$pageno，
                    string paramResidual = ds.Tables[i].Rows[j]["param_residual"].ToString() == "-" ? "" : ds.Tables[i].Rows[j]["param_residual"].ToString(); ;//列表固定后缀参数
                    string keywords = ds.Tables[i].Columns.Contains("keywords") ? ds.Tables[i].Rows[j]["keywords"].ToString() : "1";//关键字采集源列表-关键字字段（逗号分隔用来识别是不是整个采集源所有关键字已采集结束）
                    string keyword = ds.Tables[i].Columns.Contains("keyword") ? UrlEncode(ds.Tables[i].Rows[j]["keyword"].ToString()) : "";//关键字采集源列表-拆分后单个关键字（用来循环采集）
                    int seqNo = ds.Tables[i].Columns.Contains("seq_no") ? int.Parse(ds.Tables[i].Rows[j]["seq_no"].ToString()) : 1;         //关键字采集源列表序号
                    string url = gatherUrl.Replace("$param1", GetTranslationStringDate(listP1)).Replace("$param2", GetTranslationStringDate(listP2)).Replace("$kw", keyword);//动态参数格式
                    url += paramResidual;//包含页面变量用来循环地址
                    string infoURL = ds.Tables[i].Rows[j]["info_url"].ToString();//信息表参数化,获取网址的时候替换   
                    string urlPattern = ds.Tables[i].Rows[j]["info_url"].ToString();//列表网址拼接格式
                    string listBegin = ds.Tables[i].Rows[j]["list_begin"].ToString(); //列表开始字符串
                    string listEnd = ds.Tables[i].Rows[j]["list_end"].ToString(); //列表结束字符串
                    int firstPageRatio = int.Parse(ds.Tables[i].Rows[j]["first_page_ratio"].ToString());
                    int firstPagePlus = int.Parse(ds.Tables[i].Rows[j]["first_page_plus"].ToString());
                    int firstPage = int.Parse(ds.Tables[i].Rows[j]["first_page"].ToString());//采集的第一页，大于1则表示首页信息和翻页信息不能通用格式
                    int gatherPages = int.Parse(ds.Tables[i].Rows[j]["gather_pages"].ToString());//非所有页时,实际采集页数
                    firstPage = firstPage * firstPageRatio + firstPagePlus; //循环的起始页
                    gatherPages = gatherPages * firstPageRatio + firstPagePlus;//循环的采集总页数
                    int totalPages = int.Parse(ds.Tables[i].Rows[j]["total_pages"].ToString());//列表总页数
                    bool isGatherAllPages = (bool)ds.Tables[i].Rows[j]["is_gather_all_pages"];//是否采集所有页面
                    bool isGenericGatherUrl = (bool)ds.Tables[i].Rows[j]["is_generic_gather_url"];//是否采集所有页面
                    string listPattern = ds.Tables[i].Rows[j]["list_pattern"].ToString().Replace("单引号", @"'").Replace("双引号", @"""").Replace("斜杠", @"\");//列表页面正则
                    string listCharset = ds.Tables[i].Rows[j]["list_charset"].ToString();//列表页面字符集
                    bool listIsPost = (bool)ds.Tables[i].Rows[j]["list_ispost"];//是否采集所有页面
                    int gatherType = int.Parse(ds.Tables[i].Rows[j]["gather_type"].ToString()); //采集类型 0：仅列表采集  1：采集信息页面（可单独GET采集，不需要cookies等）2：采集信息页面（必须连续采集）
                    string infoPattern = ds.Tables[i].Rows[j]["info_pattern"].ToString();
                    string infoCharset = ds.Tables[i].Rows[j]["info_charset"].ToString();
                    string infoRequestHeader = ds.Tables[i].Rows[j]["info_request_header"].ToString();
                    string infoFixedFields = ds.Tables[i].Rows[j]["info_fixed_fields"].ToString();
                    string infoVarFields = ds.Tables[i].Rows[j]["info_var_fields"].ToString();
                    string infoParamsFields = ds.Tables[i].Rows[j]["info_params_fields"].ToString();

                    #endregion

                    dtList = CreateDatatableList(listPattern);
                    GatherList(dtList, sourceID, listOpcID, provinceID, classID, sourceUrl, keyword, seqNo, url, infoURL, urlPattern, listBegin, listEnd, firstPageRatio, firstPage, gatherPages, totalPages, isGatherAllPages, isGenericGatherUrl, listPattern, listCharset, listIsPost, infoPattern, infoCharset, infoRequestHeader, infoOpcID, gatherType, infoFixedFields, infoVarFields, infoParamsFields);

                    //0:仅列表采集  1：采集信息到Waiting信息表（通过改变配置 "release"=>releaseConfig="120" 或"debug"=> 输入待采信息ID 执行采集）  2：采集完列表后立即采集信息
                    if (gatherType == 2 && keywords.Split('|').Length == seqNo)
                    {
                        SyncModelTable("pr_sync_gather_list_model");
                        //采集列表后，直接获取待采池子里面的对应此sourceID数据，然后调用信息采集
                        DataSet _ds = DAL.GetAllItemSource("select * from t_gather_infopage_waiting where status=0 and ins_time>cast( dateadd(dd,-7,getdate()) as date) and source_id=" + sourceID.ToString(), CommandType.Text);
                        if (_ds.Tables[0].Rows.Count > 0)
                        {
                            InfoPageMaster(_ds);
                        }
                    }
                }
                Console.WriteLine("\n\n------------- 本批次列表采集全部完成 ----------------");
                SyncModelTable("pr_sync_gather_list_model");
                Console.WriteLine("--------------------------------------------------------------------------------------\n");
            }
          
        }

        /// <summary>
        /// 信息采集主程序（DS中列的个数是变化的）
        /// </summary>
        /// <param name="ds"></param>
        private static void InfoPageMaster(DataSet ds)
        {

            List<string> list = new List<string>();

            //创建用于批量插入到数据库模板表的DATATABLE
            if (dtInfoPage == null)
            {
                dtInfoPage = CreateDatatableInfoPage(ds.Tables[0], out list);
            }

            //根据Waiting表上面的正则，转换为匹配的具体数值
            GatherInfoPage(ds.Tables[0]);

            //将数据放入到信息模板表里面
            DataView dv = ds.Tables[0].DefaultView;
            //dv.RowFilter = "len(gather_result)=0";
            dtInfoPage = dv.ToTable(false, list.ToArray());


            //插入到模板表
            DAL.LoadDataTableToDBModelTable(dtInfoPage, "t_gather_infopage_model");

            //处理完本次数据重置容器
            dtInfoPage = null;

            //数据库模板表数据分配到各个具体信息页面表
            SyncModelTable("pr_sync_gather_infopage_model");
        }

        /// <summary>
        /// 列表采集源采集主功能函数
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="sourceID"></param>
        /// <param name="listOpcID"></param>
        /// <param name="provinceID"></param>
        /// <param name="classID"></param>
        /// <param name="sourceUrl"></param>
        /// <param name="keyword"></param>
        /// <param name="seqNo"></param>
        /// <param name="url"></param>
        /// <param name="infoUrl"></param>
        /// <param name="urlPattern"></param>
        /// <param name="listBegin"></param>
        /// <param name="listEnd"></param>
        /// <param name="firstPageRatio"></param>
        /// <param name="firstPage"></param>
        /// <param name="gatherPages"></param>
        /// <param name="totalPages"></param>
        /// <param name="isGatherAllPages"></param>
        /// <param name="isGenericGatherUrl"></param>
        /// <param name="listPattern"></param>
        /// <param name="listCharset"></param>
        /// <param name="listIsPost"></param>
        /// <param name="infoPattern"></param>
        /// <param name="infoCharset"></param>
        /// <param name="infoRequestHeader"></param>
        /// <param name="infoOpcID"></param>
        /// <param name="gatherType"></param>
        /// <param name="infoFixedFields"></param>
        /// <param name="infoVarFields"></param>
        /// <param name="infoParamsFields"></param>
        private static void GatherList(DataTable dt, int sourceID, int listOpcID, int provinceID, int classID, string sourceUrl, string keyword, int seqNo, string url, string infoUrl, string urlPattern, string listBegin, string listEnd, int firstPageRatio, int firstPage, int gatherPages, int totalPages, bool isGatherAllPages, bool isGenericGatherUrl, string listPattern, string listCharset, bool listIsPost, string infoPattern, string infoCharset, string infoRequestHeader, int infoOpcID, int gatherType, string infoFixedFields, string infoVarFields, string infoParamsFields)
        {
            //不是通用的采集网址，先首页采集
            if (!isGenericGatherUrl)
            {
                Console.WriteLine("sourceID: " + sourceID.ToString() + "\t\t pages:-1" + "\t\t 关键字:" + seqNo.ToString() + " - " + UrlDecode(keyword));
                GetListOnly(dt, classID, listOpcID, provinceID, sourceID, seqNo, keyword, sourceUrl, listIsPost, listCharset, listPattern, firstPage - 1, infoUrl, urlPattern, listBegin, listEnd, infoPattern, infoCharset, infoRequestHeader, infoOpcID, gatherType, infoFixedFields, infoVarFields, infoParamsFields);
            }


            if (isGatherAllPages)
            {
                for (int m = firstPage; m <= totalPages; m++)
                {
                    Console.WriteLine("source_id: " + sourceID.ToString() + "\t\t pages:" + m.ToString() + "\t\t 关键字:" + seqNo.ToString() + " - " + UrlDecode(keyword));
                    GetListOnly(dt, classID, listOpcID, provinceID, sourceID, seqNo, keyword, url.Replace("$pageno", firstPage.ToString()), listIsPost, listCharset, listPattern, m, infoUrl, urlPattern, listBegin, listEnd, infoPattern, infoCharset, infoRequestHeader, infoOpcID, gatherType, infoFixedFields, infoVarFields, infoParamsFields);
                }

            }
            else  //如果不是采集所有页，则把【$pageno】-->【gather_pages】页数，循环
            {

                for (int m = firstPage; m < firstPage + gatherPages; m += firstPageRatio)
                {
                    Console.WriteLine("sourceID: " + sourceID.ToString() + "\t\t pages:" + m.ToString() + "\t\t 关键字:" + seqNo.ToString() + " - " + UrlDecode(keyword));
                    GetListOnly(dt, classID, listOpcID, provinceID, sourceID, seqNo, keyword, url.Replace("$pageno", m.ToString()), listIsPost, listCharset, listPattern, m, infoUrl, urlPattern, listBegin, listEnd, infoPattern, infoCharset, infoRequestHeader, infoOpcID, gatherType, infoFixedFields, infoVarFields, infoParamsFields);
                }

            }

            //插入到通用列表模板表  t_gather_infopage_model
            DAL.LoadDataTableToDBModelTable(dt, "t_gather_list_model");
        }

        /// <summary>
        /// 信息页面采集主功能函数
        /// </summary>
        /// <param name="dt"></param>
        private static void GatherInfoPage(DataTable dt)
        {
            string p1; string response; bool isException;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                response = GetData(dt.Rows[i]["info_url"].ToString(), dt.Rows[i]["info_charset"].ToString(), out isException);

                //访问URL正常返回数据
                if (!isException)
                {
                    string errorStrings = string.Empty;
                    string errorString = string.Empty;
                    response = DealwithResponse(response);
                    foreach (var item in dt.Rows[i]["info_var_fields"].ToString().Split(','))
                    {
                        dt.Rows[i][item] = GetInfoPageData(response, dt.Rows[i][item].ToString(), item, out errorString);
                        errorStrings += errorString;
                    }
                    dt.Rows[i]["gather_result"] = errorStrings;

                    if (errorStrings.Length > 0)
                    {
                        dt.Rows[i]["status"] = 2;   //采集失败
                    }
                    else
                    {
                        dt.Rows[i]["status"] = 1;   //采集成功
                        dt.Rows[i]["parent_id"] = dt.Rows[i]["id"].ToString();
                    }
                }
                else //访问异常
                {
                    dt.Rows[i]["status"] = 2;
                    dt.Rows[i]["gather_result"] = response;
                }
            }
        }

        /// <summary>
        /// 创建存储采集数据列表的容器DataTable
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static DataTable CreateDatatableList(string pattern)
        {
            DataTable dt = new DataTable("list");
            Regex rg = new Regex(@"\(\?<(.*?)>", RegexOptions.IgnoreCase);
            if (rg.IsMatch(pattern))
            {
                MatchCollection matches = rg.Matches(pattern);

                for (int i = 0; i < matches.Count; i++)
                {
                    DataColumn dc = new DataColumn();
                    dc.ColumnName = matches[i].Groups[1].Value.ToString();
                    //正则用“|”将数据统一到同一列上的时候，会需要多次用到同一个列，此处只添加一次
                    if (!dt.Columns.Contains(dc.ColumnName))
                    {
                        dt.Columns.Add(dc);
                    }
                }
                //没有发布日期，正则就不会生成此列，导致后面赋值判断异常
                if (!dt.Columns.Contains("publish_date"))
                {
                    dt.Columns.Add("publish_date");
                }

                //添加url列
                if (!dt.Columns.Contains("url"))
                {
                    dt.Columns.Add("url");
                }
                //添加标题列列
                if (!dt.Columns.Contains("info_title"))
                {
                    dt.Columns.Add("info_title");
                }

                //除了正则列和必须的列以外，列表固定列
                dt.Columns.Add("source_id");
                dt.Columns.Add("class_id");
                dt.Columns.Add("list_opc_id");
                dt.Columns.Add("info_opc_id");
                dt.Columns.Add("province_id");
                dt.Columns.Add("gather_type");
                dt.Columns.Add("url_pattern");
                dt.Columns.Add("keyword");
                dt.Columns.Add("seq_no");

                //除了正则列和必须的列以外，信息固定列
                dt.Columns.Add("info_pattern");
                dt.Columns.Add("info_charset");
                dt.Columns.Add("info_request_header");
                dt.Columns.Add("info_fixed_fields");
                dt.Columns.Add("info_var_fields");
                dt.Columns.Add("info_params_fields");
            }
            return dt;
        }

        /// <summary>
        /// 创建存储采集数据具体信息表的容器DataTable
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static DataTable CreateDatatableInfoPage(DataTable dtInfoPageWaiting, out List<string> outFieldsInSingleColumn)
        {
            DataTable dt = new DataTable("infopage");
            List<string> fields = new List<string>();
            List<string> FieldsInSingleColumn = new List<string>();
            string f1; string f2; string f3;

            for (int i = 0; i < dtInfoPageWaiting.Rows.Count; i++)
            {
                f1 = dtInfoPageWaiting.Rows[i]["info_fixed_fields"].ToString();
                f2 = dtInfoPageWaiting.Rows[i]["info_var_fields"].ToString();
                //f3 = dtInfoPageWaiting.Rows[i]["info_params_fields"].ToString();
                fields.Add(f1);
                fields.Add(f2);
                //fields.Add(f3);
            }

            foreach (var items in fields.Distinct().ToList())
            {
                var item = items.Split(',');
                foreach (var item1 in item)
                {
                    DataColumn dc = new DataColumn
                    {
                        ColumnName = item1.ToString()
                    };

                    if (!dt.Columns.Contains(dc.ColumnName))
                    {
                        dt.Columns.Add(dc);
                        FieldsInSingleColumn.Add(dc.ToString());
                    }
                }
            }
            outFieldsInSingleColumn = FieldsInSingleColumn;
            return dt;
        }

        /// <summary>
        /// POST方式获取网址响应
        /// </summary>
        /// <param name="sourceurl"></param>
        /// <param name="charset"></param>
        /// <param name="isException"></param>
        /// <returns></returns>
        private static string PostData(string sourceurl, string charset, out bool isException)
        {
            try
            {
                //数据库网址URL以？号分割, POSTformdata里面也有|，因此不能用split
                string postData = sourceurl.Split(new char[] { '|' }, 2)[1];
                string url = sourceurl.Split(new char[] { '|' }, 2)[0];

                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] data = encoding.GetBytes(postData);

                //byte[] data = Encoding.Unicode.GetBytes(postData);
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                myRequest.Timeout = 20000;
                myRequest.Method = "POST";
                //myRequest.ContentType = "application/x-www-form-urlencoded;"+charsetparam;
                myRequest.ContentType = "application/x-www-form-urlencoded";

                myRequest.ContentLength = data.Length;
                //myRequest.CookieContainer = new CookieContainer();

                Stream newStream = myRequest.GetRequestStream();

                //多线程超时解决办法
                //System.Net.ServicePointManager.DefaultConnectionLimit = 50;
                newStream.Write(data, 0, data.Length);
                newStream.Close();
                HttpWebResponse myResponse = null;

                try
                {
                    myResponse = (HttpWebResponse)myRequest.GetResponse();
                    StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.GetEncoding(charset));//乱码需要转码
                    string content = reader.ReadToEnd();
                    reader.Close();

                    //避免超时
                    myResponse.Close();
                    isException = false;
                    return content;
                }
                catch (Exception ex)
                {
                    isException = true;
                    //取消访问，关闭响应，避免超时
                    myRequest.Abort();
                    if (myResponse != null)
                    {
                        myResponse.Close();
                    }

                    return ex.Message.ToString();

                }

            }
            catch (Exception e)
            {
                isException = true;
                return e.Message.ToString();
            }

        }

        /// <summary>
        /// GET方式获取网址响应
        /// </summary>
        /// <param name="url"></param>
        /// <param name="charset"></param>
        /// <param name="isException"></param>
        /// <returns></returns>
        private static string GetData(string url, string charset, out bool isException)
        {
            try
            {
                Util.SetCertificatePolicy();//验证安全，未能为 SSL/TLS 安全通道建立信任关系
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                myRequest.Timeout = 20000;
                myRequest.Method = "GET";
                HttpWebResponse myResponse = null;
                try
                {
                    myResponse = (HttpWebResponse)myRequest.GetResponse();
                    StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.GetEncoding(charset));
                    string content = reader.ReadToEnd();
                    reader.Close();
                    myResponse.Close();
                    isException = false;
                    return content;
                }
                catch (Exception e)
                {
                    //取消访问，关闭响应，避免超时
                    myRequest.Abort();
                    if (myResponse != null)
                    {
                        myResponse.Close();
                    }

                    isException = true;
                    return e.Message.ToString();
                }
            }
            catch (Exception e)
            {
                isException = true;
                return e.Message.ToString();
            }



        }

        //不太清楚干嘛的，百度上的
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {   // 总是接受  
            return true;
        }

        /// <summary>
        /// 获取列表上的数据主函数
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="sourceid"></param>
        /// <param name="url"></param>
        /// <param name="ispost"></param>
        /// <param name="listCharset"></param>
        /// <param name="pattern"></param>
        /// <param name="pageNo"></param>
        /// <param name="infoUrl"></param>
        /// <param name="urlPattern"></param>
        /// <param name="listBegin"></param>
        /// <param name="listEnd"></param>
        /// <returns></returns>
        private static DataTable GetListOnly(DataTable dt, int classID, int listOpcID, int provinceID, int sourceID, int seqNo, string keyword, string url, bool listIsPost, string listCharset, string listPattern, int pageNo, string infoUrl, string urlPattern, string listBegin, string listEnd, string infoPattern, string infoCharset, string infoRequestHeader, int infoOpcID, int gatherType, string infoFixedFields, string infoVarFields, string infoParamsFields)
        {

            #region 1.采集过程

            //采集开始时间
            string gatherBT = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            bool isRegMatch = false;//是否匹配到数据
            bool isException;//HTTP获取数据是否异常
            string response = string.Empty;//获取响应内容或者异常信息
            int gatherRows = 0;//正则匹配到的条数基本等于每个列表页面的链接数
            if (listIsPost)
            {
                response = PostData(url, listCharset, out isException);
            }
            else
            {
                //下列采集列表只能使用精简版的GET（具体原因没有去研究）
                if (sourceID == 52 || sourceID == 53 || sourceID == 33)
                {
                    response = test.Get(url, listCharset, out isException);
                }
                else
                {
                    response = GetData(url, listCharset, out isException);
                }
            }
            //没有异常
            if (!isException)
            {
                response = DealwithResponse(response);
                Regex rg = new Regex(listPattern, RegexOptions.IgnoreCase);//指定不区分大小写的匹配
                if (response != null || response.Length > 0)
                {
                    if (listBegin.Length > 0 && listEnd.Length > 0)
                    {
                        response = SubstringResponse(response, listBegin, listEnd);
                    }

                    //bool isMatch = rg.IsMatch(_content);
                    isRegMatch = rg.IsMatch(response);
                    if (isRegMatch)
                    {
                        MatchCollection matches = rg.Matches(response);
                        gatherRows = matches.Count;
                        for (int i = 0; i < gatherRows; i++)
                        {
                            Match matchitem = matches[i];
                            GroupCollection gc = matchitem.Groups;
                            DataRow dr = dt.NewRow();

                            //多少个匹配组就创建多少个列值
                            for (int j = 1; j < gc.Count; j++)
                            {
                                dr[j - 1] = gc[j];
                            }
                            //清理标题格式
                            dr["info_title"] = DealWithTitle(dr["info_title"].ToString());


                            //防止日期采集出问题,
                            dr["publish_date"] = DealWithPublishDate(dr["publish_date"].ToString(), url);

                            //处理特殊的网址格式
                            bool isDealed = false;
                            dr["url"] = DealWithUrl(urlPattern, dr["url"].ToString(), out isDealed);

                            //已经处理成完整的URL，写入数据库的信息网址构成样式，直接用$url代替;
                            dr["url_pattern"] = isDealed ? "$url" : urlPattern;


                            //固定必须有的数据（采集源）
                            dr["source_id"] = sourceID.ToString();
                            dr["class_id"] = classID.ToString();
                            dr["list_opc_id"] = listOpcID.ToString();
                            dr["info_opc_id"] = infoOpcID.ToString();
                            dr["province_id"] = provinceID.ToString();
                            dr["keyword"] = UrlDecode(keyword);
                            dr["seq_no"] = seqNo.ToString();
                            dr["info_pattern"] = infoPattern;
                            dr["info_charset"] = infoCharset;
                            dr["info_request_header"] = infoRequestHeader;
                            dr["gather_type"] = gatherType.ToString();
                            dr["info_fixed_fields"] = infoFixedFields;
                            dr["info_var_fields"] = infoVarFields;
                            dr["info_params_fields"] = infoParamsFields;
                            dt.Rows.Add(dr);
                        }
                    }
                }

            }

            //采集结束时间
            string gatherET = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");


            #endregion

            #region 2.处理需要写入到数据库的数据格式
            //没有匹配到的情况下，将响应保存到数据库
            response = isRegMatch ? "" : response;
            listPattern = isRegMatch ? "" : listPattern;
            #endregion

            #region 3.将采集日志写入到数据库表
            int cnt = 1;
            int ok = DAL.WriteLog(classID, sourceID, seqNo, UrlDecode(keyword), pageNo, isRegMatch, isException, response, listPattern, gatherBT, gatherET, gatherRows);
            while (ok != 1 && cnt < 3)
            {
                Console.WriteLine($" 第{cnt}次 重试写入采集日志表...");
                ok = DAL.WriteLog(classID, sourceID, seqNo, UrlDecode(keyword), pageNo, isRegMatch, isException, response, listPattern, gatherBT, gatherET, gatherRows);
                cnt++;
            }
            #endregion

            return dt;

        }

        /// <summary>
        /// 列表是动态访问参数
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private static string GetTranslationStringDate(string param)
        {
            string _result = "";
            switch (param)
            {
                case "yyyy-01-01": _result = DateTime.Now.Year.ToString() + "-01-01"; break;
                case "now-yyyy-mm-dd": _result = DateTime.Now.ToString("yyyy-MM-dd"); break;
                case "now-yyyy-mm-dd-lastmonth": _result = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd"); break;
                default:
                    break;
            }
            return _result;
        }

        /// <summary>
        /// 根据获取列表cookie，获取具体信息页面响应（目前没有用）
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="postDataStr"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        private static string SendDataByGET(string Url, string postDataStr, ref CookieContainer cookie)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
            if (cookie.Count == 0)
            {
                request.CookieContainer = new CookieContainer();
                cookie = request.CookieContainer;
            }
            else
            {
                request.CookieContainer = cookie;
            }

            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        /// <summary>
        /// 通过开始和结束位置，先截取中间的内容，再匹配具体正则
        /// </summary>
        /// <param name="content"></param>
        /// <param name="listbegin"></param>
        /// <param name="listend"></param>
        /// <returns></returns>
        private static string SubstringResponse(string content, string listbegin, string listend)
        {
            //Regex rg = new Regex(listbegin + @"(.*?)" + listend);
            //return rg.Match(content).Groups[1].ToString();
            Regex rg = new Regex(listbegin + ".*?" + listend);
            return rg.Match(content).ToString();
        }

        /// <summary>
        /// 原始内容处理（去空值和脚本内容）
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static string DealwithResponse(string content)
        {
            string result = null;
            content = content.Replace("\t", "").Replace("\r", "").Replace("\n", "").Replace("&nbsp;", "");

            //将内容中的脚本去除
            content = Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(content, @"<script.*?</.*?script.*?>", ""), @"replace.*?\(.*?\)", ""), "var.*?/.*?/", ""), "<!--.*?-->", "");
            try
            {
                //将Unicode标识\U6278 转换为中文
                result = Regex.Unescape(content);
            }
            catch
            {
                result = content;
            }
            return result;


        }

        /// <summary>
        /// 验证程序运行时，控制台输入值是否合法
        /// </summary>
        /// <param name="input"></param>
        /// <param name="ifLegal"></param>
        /// <returns></returns>
        private static string DealWithInput(string input, out bool ifLegal)
        {
            string result = "0";
            input = DealWithBlank(input);
            if (Int64.TryParse(input.Replace(",", ""), out _))
            {
                string[] items = input.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                result = string.Join(",", items);
                ifLegal = true;
            }
            else
            {
                ifLegal = false;
            }

            return result;
        }

        /// <summary>
        /// 处理空白符和后台源代码中的空格
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string DealWithBlank(string input)
        {
            if (input != null)
            {
                return input.Replace("\t", "").Replace("\r", "").Replace("\n", "").Replace(" ", ",");
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// 处理采集的标题格式
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private static string DealWithTitle(string title)
        {
            //string result = Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(title.ToLower(), "<font.*?>", ""), "</font.*?>", ""), " ", ""), "<span.*?>", ""), "</span.*?>", "");

            string result = Regex.Replace(title, "</?[a-z].*?>", "", RegexOptions.IgnoreCase);
            if (result.Length > 200)
            {
                return result.Substring(0, 200);//限制在200个字符以内
            }
            else
            {
                return result;
            }
        }

        private static string DealWithContent(string content)
        {
            return Regex.Replace(content, "</?[a-z].*?>", "", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 处理采集的发布日期格式
        /// </summary>
        /// <param name="publishDate"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string DealWithPublishDate(string publishDate, string url)
        {
            //清空发布日期中的标签
            string _publishDate = Regex.Replace(Regex.Replace(publishDate, "<a.*?</a>", ""), "<.*?>", "").Replace("年", "-").Replace("月", "-").Replace("日", "");
            DateTime _result;
            if (_publishDate.Length == 0) //如果为空，则设置为明天
            {
                return DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            }
            else if (DateTime.TryParse(_publishDate, out _result)) //可以转换为时间
            {
                return _result.ToString("yyyy-MM-dd");
            }
            else if (Int64.TryParse(_publishDate, out _)) //整数时间戳的情况下，转换为符合日期的字符串
            {
                return ConvertStringToDateTime(_publishDate).ToString("yyyy-MM-dd HH:mm:ss");

            }
            else
            {
                if (url.Contains("www.ccgp-sichuan.gov.cn"))//特殊格式的日期，处理为正常值
                {
                    try
                    {
                        return _publishDate.Substring(2, 7) + "-" + _publishDate.Substring(0, 2);
                    }

                    catch
                    {
                        return DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd");
                    }
                }
                else//采集发布时间格式有问题，设置为后天
                {
                    return DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");
                }

            }
        }

        /// <summary>
        /// 处理采集的Url格式
        /// </summary>
        /// <param name="urlpattern">信息组成样式</param>
        /// <param name="url">返回处理后的具体信息URL</param>
        /// <param name="isDealed">是否已经处理成完整的信息URL（http;//xxx.com）,是的话，就将链接样式设置为$URL</param>
        /// <returns></returns>
        private static string DealWithUrl(string urlpattern, string url, out bool isDealed)
        {

            urlpattern = urlpattern.Replace("$url", "").EndsWith("/") ? urlpattern.Replace("$url", "") : urlpattern.Replace("$url", "") + "/";
            //处理相对目录
            if (url.StartsWith("../"))
            {

                string domain = string.Empty;
                int cnt = Regex.Matches(url, @"\.\./").Count;
                string[] result = urlpattern.Split('/');
                for (int i = 0; i < result.Length - cnt - 1; i++)
                {
                    domain += result[i] + "/";


                }
                isDealed = true;
                return domain + url.Replace("../", "");
                //"http://www.hc.gov.cn/xxgk/zdgzgk/zgyzb/"
                //"../../../gggs/201811/t20181119_129192.html"

            }
            else if (url.StartsWith("./"))
            {
                isDealed = true;
                return urlpattern + url.Replace("./", "");
            }
            else
            {
                isDealed = false;
                return url;
            }
        }

        /// <summary>
        /// 将填的info_url中的关键字中文转码为带百分号的
        /// </summary>
        /// <param name="urlpattern"></param>
        /// <param name="url"></param>
        /// <param name="isDealed"></param>
        /// <returns></returns>
        private static string DealWithUrlPattern(bool isDealed, string urlPattern)
        {
            //信息页面，编码和不编码，用GETDATA访问的时候，没有任何区别。
            string a = HttpUtility.UrlDecode("%e6%8b%9b%e6%a0%87");
            string b = HttpUtility.UrlEncode("招标");
            return "s";

        }

        /// <summary>
        /// 获取采集列表类型查询语句         
        /// </summary>
        /// <param name="autoRun"></param>
        /// <returns></returns>
        private static string OnGetItemSourceListString()
        {
            return @"Select *  from t_item_source_list where status=1  ";
        }

        /// <summary>
        /// 获取采集关键字类型查询语句 
        /// </summary>
        /// <param name="autoRun"></param>
        /// <returns></returns>
        private static string OnGetSearchKeywordListString()
        {
            return @" select  t.*  ,value as keyword,seq as seq_no
                                    from t_search_keyword_list t cross apply dbo.fn_clr_split_by_separator(keywords,'|') as a
                                    where status=1   ";
        }

        private static string OnGetInfoPageWaitingString()
        {
            return @"select * from t_gather_infopage_waiting where status = 0 and ins_time> cast(dateadd(dd, -7, getdate()) as date)  ";
            //ValidateInput("debug", queryString);

        }

        private static string OnGetValidInputGatherTypeAndString(out string gatherType)
        {
            gatherType = ValidateInputGatherType();
            return ValidateInput(gatherType);
        }

        /// <summary>
        /// 验证用户输入结果，获取带过滤条件的字符串格式的查询
        /// </summary>
        /// <param name="autoRun"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        private static string ValidateInput(string gatherType)
        {
            bool legal = false;
            string input = string.Empty, queryString = string.Empty, message = string.Empty;
            string queryString1 = string.Empty, queryString2 = string.Empty, queryString3 = string.Empty;
            if (gatherType == "1")
            {
                message = "\n\n\n=====  请输入列表采集源ID，多个值用逗号分割，ENTER键 确认";
                queryString1 = OnGetItemSourceListString();
                queryString2 = OnGetSearchKeywordListString();
            }
            else
            {
                message = "\n=====  请输入待采信息Waiting表信息ID，多个值用逗号分割，ENTER键 确认";
                queryString3 = OnGetInfoPageWaitingString();
            }

            Console.WriteLine(message);
            do
            {
                input = Console.ReadLine().Replace("，",",");
                input = input.EndsWith(",",StringComparison.OrdinalIgnoreCase) ? input.Substring(0, input.Length - 1) : input;
                foreach (var item in input.Split(new string[] { ","}, StringSplitOptions.None))
                {
                    legal = int.TryParse(item, out _);
                    if (!legal)
                    {
                        Console.WriteLine("\t   ~888~ 请输入正确合法的数字ID ...  ");
                        break;
                    }

                }
            }
            while (!legal);
            Console.WriteLine("\n");
            queryString1 = string.IsNullOrEmpty(queryString1) ? "" : queryString1 + " and id in(" + input + ");  ";
            queryString2 = string.IsNullOrEmpty(queryString2) ? "" : queryString2 + " and id in(" + input + ");  ";
            queryString3 = string.IsNullOrEmpty(queryString3) ? "" : queryString3 + " and id in(" + input + ");  ";
            return queryString1 + queryString2 + queryString3;
        }

        private static string ValidateInputGatherType()
        {
            string input = string.Empty;
            do
            {
                Console.WriteLine("\n======  请输入序号： < 1 -- 列表采集 > < 2 -- 信息采集 >   ###==>  ENTER键 确认");
                input = Console.ReadLine();
            }
            while (input != "1" && input != "2");
            return input;
        }

        /// <summary>
        /// 中文转百分号格式
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        private static string UrlEncode(string keyword)
        {
            return HttpUtility.UrlEncode(keyword);
        }

        /// <summary>
        /// 百分号格式转中文
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        private static string UrlDecode(string keyword)
        {
            return HttpUtility.UrlDecode(keyword);

        }

        /// <summary>
        /// 将数字格式的时间戳转换为日期格式
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        private static DateTime ConvertStringToDateTime(string timeStamp)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(Regex.Replace(timeStamp, @"\s", "") + "0000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }

        /// <summary>
        /// 将t_gather_list_model数据分发到对应的列表Table
        /// </summary>
        /// <returns></returns>
        private static bool SyncModelTable(string procedureName)

        {
            string type = procedureName.Contains("list") ? "列表" : "信息页面";
            int callCount = 1;

            //同步到信息表,监控时间
            Stopwatch sw = new Stopwatch();
            sw.Start(); //开启计时器

            bool ok = DAL.ExecProcedureNonParamerters(procedureName);
            while (!ok && callCount < 3)
            {
                Console.WriteLine($"\n\t调用 < 同步{type}过程 > 失败    时间：" + string.Format("{0} ms", sw.ElapsedMilliseconds) + $" 重复第 {callCount} 次调用...");
                callCount++;

                ok = DAL.ExecProcedureNonParamerters(procedureName);
            }
            sw.Stop(); //停止计时器

            if (ok)
            {
                Console.WriteLine($"\n\t调用 < 同步{type}数据 > 成功!!!\t执行总时间：" + string.Format("{0} ms", sw.ElapsedMilliseconds) + "\n\r");
                return true;
            }
            else
            {
                Console.WriteLine($"\n\t调用 < 同步{type}数据 > 失败!!!\t执行总时间：" + string.Format("{0} ms", sw.ElapsedMilliseconds) + "\t 半小时内系统自动执行...\n\r");
                return false;
            }
        }

        /// <summary>
        /// 处理具体信息页面所有需要采集的字段值
        /// </summary>
        /// <param name="item"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string DealWithInfoPageFields(string item, string text)
        {
            if (item == "info_title")
            {
                return DealWithTitle(text);
            }
            else if (item == "info_content")
            {
                return DealWithContent(text); ;
            }
            else if (item == "publish_date")
            {
                return DealWithPublishDate(text, "");
            }
            else
            {
                return text;
            }
        }

        /// <summary>
        /// 将Waiting表里面的正则样式转化为具体数值返回
        /// </summary>
        /// <param name="response"></param>
        /// <param name="pattern"></param>
        /// <param name="field"></param>
        /// <param name="outErrorFiels"></param>
        /// <returns></returns>
        private static string GetInfoPageData(string response, string pattern, string field, out string outErrorFiels)
        {
            string begin = string.Empty;
            string end = string.Empty;
            string r = string.Empty;
            foreach (var group in pattern.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (group.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Length == 2)
                {
                    begin = group.Split('|')[0];
                    end = group.Split('|')[1];
                    Regex rg = new Regex(begin + @"(.*?)" + end);
                    if (rg.IsMatch(response))
                    {

                        r = rg.Match(response).Groups[1].ToString();
                        if (r.Length > 0)
                        {
                            outErrorFiels = null;
                            return DealWithInfoPageFields(field, r);
                        }
                        else
                        {
                            //匹配到了内容为空
                            outErrorFiels = null;
                            return "正则匹配成功 < 内容为空 >";
                        }
                    }
                }
                else
                {
                    outErrorFiels = field + " -- 正则格式错误";
                    return pattern;
                }

            }
            outErrorFiels = field + " -- 正则匹配失败   ";
            return pattern;
        }
    }
}
