using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Configuration;
using System.Web;
using System.Threading;
using System.Net.Cache;
using System.Reflection;
using System.Collections.Specialized;
using System.Collections;
using System.IO.Compression;
using System.Globalization;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace 控制台程序获取数据
{
    class Program
    {
        //全局变量
        static readonly string releaseConfig = ConfigurationManager.AppSettings["releaseConfig"];
        static readonly string runModel = ConfigurationManager.AppSettings["runModel"];
        static readonly int batchCount = int.Parse(ConfigurationManager.AppSettings["batchCount"]);
        static readonly int gatherSpeed = int.Parse(ConfigurationManager.AppSettings["gatherSpeed"]);
        static readonly bool ifDESC = bool.Parse(ConfigurationManager.AppSettings["ifDESC"]);
        static string viewStateStringGlobal = string.Empty;//WebForm格式的网站，获取前一个页面的状态，当做FormData传递访问
        static string dynamicFormDataGlobal = string.Empty;//FormData里面数据是动态生成的，使用情况（1：获取Cookie网址和动态参数网址相同，则在获取Cookie方法里面获取；2.如果没Cookie网址只需要动态网址，则用GetData方法获取）
        static string cookieStringsGlobal = string.Empty;//静态cookie,获取到此cookie后可以直接访问（如果还需界面动态cookie的话，此列表数据就用WebBrowser获取）

        /// <summary>
        /// 程序主函数：程序开始到程序结束；主线程才可以调用WebBrowser？？？
        /// </summary>
        /// <param name="args"></param>
        [STAThread]
        static void Main()
        {

            //使用WebBrowser必须加WinFrom环境
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            //Form1 frm2 = new Form1("http://www.chinaunicombidding.cn/jsp/cnceb/web/info1/infoList.jsp?page=1");
            //string xx = GetData("http://xmzwggzy.xlgl.gov.cn/xmweb/showinfo/zbgg_moreNew.aspx?categoryNum=009001005001&categoryNum2=009002006001&categoryNum3=009003003001&categoryNum4=009004003001&categoryNum5=009002008&Paging=1", out _);
            //string xx1 = GetData("http://222.85.133.14:8899/noticeconstruct/index.htm", out _);
            //string aa1 = GetData("http://www.chinaunicombidding.cn/jsp/cnceb/web/info1/infoList.jsp?page=2", out _);
            //string ss = GetDataByWebBrowser("http://www.chinaunicombidding.cn/jsp/cnceb/web/info1/infoList_all.jsp?notice=&time1=&time2=&province=&city=");//首页
            //string ss1 = GetDataByWebBrowser("http://www.chinaunicombidding.cn/jsp/cnceb/web/info1/infoList.jsp?page=2");//第二页

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
            string queryString = OnGetValidInputGatherTypeAndString(out string gatherType);
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
            DataSet ds;
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

            int listCount = 0;
            for (int i = 0; i < ds.Tables.Count; i++)
            { int count = ds.Tables[i].Rows.Count;
                for (int j = 0; j < count; j++)
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
                    string paramResidual = ds.Tables[i].Rows[j]["param_residual"].ToString() == "-" ? "" : ds.Tables[i].Rows[j]["param_residual"].ToString();//列表固定后缀参数

                    string listCharset = ds.Tables[i].Rows[j]["list_charset"].ToString();//列表页面字符集

                    string keywords = ds.Tables[i].Columns.Contains("keywords") ? ds.Tables[i].Rows[j]["keywords"].ToString() : "1";//关键字采集源列表-关键字字段（逗号分隔用来识别是不是整个采集源所有关键字已采集结束）
                    string keyword = ds.Tables[i].Columns.Contains("keyword") ? UrlEncode(ds.Tables[i].Rows[j]["keyword"].ToString(), Encoding.GetEncoding(listCharset)) : "";//关键字采集源列表-拆分后单个关键字（用来循环采集）
                    //此网站搜索关键做了一个编码转换，完善此功能没什么意义，直接用特例处理
                    if (sourceID == 100077)//GBK编码
                    {   //处罚 、 国网、 电力、 电网
                        keyword = keyword
                                .Replace("%e5%a4%84%e7%bd%9a", "5aSE572a").Replace("%e5%9b%bd%e7%bd%91", "5Zu9572R").Replace("%e7%94%b5%e5%8a%9b", "55S15Yqb").Replace("%e7%94%b5%e7%bd%91", "55S1572R")//utf-8
                                .Replace("%b4%a6%b7%a3", "5aSE572a").Replace("%b9%fa%cd%f8", "5Zu9572R").Replace("%b5%e7%c1%a6", "55S15Yqb").Replace("%b5%e7%cd%f8", "55S1572R"); //gbk、gb2312
                    }
                    int seqNo = ds.Tables[i].Columns.Contains("seq_no") ? int.Parse(ds.Tables[i].Rows[j]["seq_no"].ToString()) : 1;         //关键字采集源列表序号
                    string url = gatherUrl.Replace("$param1", GetTranslationStringDate(listP1)).Replace("$param2", GetTranslationStringDate(listP2)).Replace("$kw", keyword);//动态参数格式
                    url += paramResidual;//包含页面变量用来循环地址
                    string urlPattern = ds.Tables[i].Rows[j]["info_url"].ToString();//列表网址拼接格式,信息表参数化,获取网址的时候替换   
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

                    bool listIsPost = (bool)ds.Tables[i].Rows[j]["list_ispost"];//是否采集所有页面
                    int gatherType = int.Parse(ds.Tables[i].Rows[j]["gather_type"].ToString()); //采集类型 0：仅列表采集  1：采集信息页面（可单独GET采集，不需要cookies等）2：采集信息页面（必须连续采集）
                    //string infoPattern = ds.Tables[i].Rows[j]["info_pattern"].ToString();   //忘记为什么增加这参数了,没有用到
                    string infoCharset = ds.Tables[i].Rows[j]["info_charset"].ToString(); //已弃用
                    string infoRequestHeader = ds.Tables[i].Rows[j]["info_request_headers"].ToString();//信息请求Headers???感觉没有必要
                    string infoFixedFields = ds.Tables[i].Rows[j]["info_fixed_fields"].ToString();//生成信息相关表的基础字段
                    string infoVarFields = ds.Tables[i].Rows[j]["info_var_fields"].ToString();//信息参数表的可变字段
                    string infoParamsFields = ds.Tables[i].Rows[j]["info_params_fields"].ToString();//信息页参数
                    string listRequestHeaders = ds.Tables[i].Rows[j]["list_request_headers"].ToString();//列表请求Headers
                    string viewStateUrl = ds.Tables[i].Columns.Contains("viewstate_url") ? ds.Tables[i].Rows[j]["viewstate_url"].ToString() : "";//获取VIEWSTATE的首页网址
                    int delayMS = (int)ds.Tables[i].Rows[j]["delay_ms"];//翻页间隔时间
                    int commonParams = int.Parse(ds.Tables[i].Rows[j]["common_params"].ToString());//通用是否参数，位与计算
                    string DynamicFormdataParamUrl = ds.Tables[i].Columns.Contains("dynamic_formdata_param_url") ? ds.Tables[i].Rows[j]["dynamic_formdata_param_url"].ToString() : "";//获取动态参数FormData的首页

                    #endregion
                    //每次都要根据正则来重建DT
                    DataTable dtList = CreateDatatableList(listPattern);
                    Console.WriteLine($"\r\nsourceID:{sourceID} \t\t\t {j}/{count+1} \t\t\t 关键字: {seqNo} - { UrlDecode(keyword, Encoding.GetEncoding(listCharset))}");
                    GatherList(dtList, commonParams, DynamicFormdataParamUrl, delayMS, viewStateUrl, sourceID, listOpcID, provinceID, classID, sourceUrl, keyword, seqNo, url, urlPattern, listBegin, listEnd, firstPageRatio, firstPage, gatherPages, totalPages, isGatherAllPages, isGenericGatherUrl, listPattern, listCharset, listIsPost, infoCharset, infoRequestHeader, infoOpcID, gatherType, infoFixedFields, infoVarFields, infoParamsFields, listRequestHeaders);

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
                        else
                        {
                            Console.WriteLine("\t没有最新的网址链接，或是没有列表list_pattern获取链接正则错误\n\n");
                        }
                    }
                    if (gatherType != 2)
                    {
                        listCount++;
                    }
                }
            }
            if (listCount > 0)
            {
                SyncModelTable("pr_sync_gather_list_model");
            }
            Console.WriteLine("\n\n------------- 本批次列表同步全部完成 ----------------");
        }

        /// <summary>
        /// 信息采集主程序（DS中列的个数是变化的）
        /// </summary>
        /// <param name="ds"></param>
        private static void InfoPageMaster(DataSet ds)
        {
            //根据Waiting表上面的正则，转换为匹配的具体数值，将数据放入到dv里面
            DataView dv = GatherInfoPage(ds.Tables[0],out List<string> allFieldsToLoad).DefaultView;

            //获取加载到数据的所有字段，对已经获取值的加载字段来个Select,插入到模板表
            DAL.LoadDataTableToDBModelTable(dv.ToTable(false, allFieldsToLoad.ToArray()), "t_gather_infopage_model");

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
        /// <param name="gatherUrl"></param>
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
        private static void GatherList(DataTable dt, int commonParams, string DynamicFormDataParamUrl, int delayMS, string viewStateUrl, int sourceID, int listOpcID, int provinceID, int classID, string sourceUrl, string keyword, int seqNo, string gatherUrl,  string urlPattern, string listBegin, string listEnd, int firstPageRatio, int firstPage, int gatherPages, int totalPages, bool isGatherAllPages, bool isGenericGatherUrl, string listPattern, string listCharset, bool listIsPost, string infoCharset, string infoRequestHeader, int infoOpcID, int gatherType, string infoFixedFields, string infoVarFields, string infoParamsFields, string listRequestHeaders)
        {
            //获取整个采集源的cookie给全局变量cookieStringsGlobal赋值
            GetCookieBySourceUrl(sourceUrl, listRequestHeaders, DynamicFormDataParamUrl, sourceID,listCharset);


            if (viewStateUrl.Length > 0)     //viewStateString = GetViewStateBySourceUrl(viewStateUrl, listCharset, listRequestHeaders, out _, cookie);
                GetListOnly(dt, commonParams, DynamicFormDataParamUrl, delayMS, true, classID, listOpcID, provinceID, sourceID, seqNo, keyword, viewStateUrl, listIsPost, listCharset, listPattern, firstPage - 1, urlPattern, listBegin, listEnd, infoCharset, infoRequestHeader, infoOpcID, gatherType, infoFixedFields, infoVarFields, infoParamsFields, listRequestHeaders);
            else
                viewStateStringGlobal = "";

            //不是通用的采集网址，先首页采集
            if (!isGenericGatherUrl)
            {
                Console.WriteLine($"pages:-1 \t {gatherUrl}");
                GetListOnly(dt, commonParams, DynamicFormDataParamUrl, delayMS, false, classID, listOpcID, provinceID, sourceID, seqNo, keyword, sourceUrl, listIsPost, listCharset, listPattern, firstPage - 1, urlPattern, listBegin, listEnd, infoCharset, infoRequestHeader, infoOpcID, gatherType, infoFixedFields, infoVarFields, infoParamsFields, listRequestHeaders);
            }


            if (isGatherAllPages)
            {
                for (int m = firstPage; m <= totalPages; m++)
                {
                    Console.WriteLine($"pages:{m} \t "+gatherUrl.Replace("$pageno", m.ToString()));
                    GetListOnly(dt, commonParams, DynamicFormDataParamUrl, delayMS, false, classID, listOpcID, provinceID, sourceID, seqNo, keyword, gatherUrl.Replace("$pageno", firstPage.ToString()), listIsPost, listCharset, listPattern, m, urlPattern, listBegin, listEnd, infoCharset, infoRequestHeader, infoOpcID, gatherType, infoFixedFields, infoVarFields, infoParamsFields, listRequestHeaders);
                }

            }
            else  //如果不是采集所有页，则把【$pageno】-->【gather_pages】页数，循环
            {

                for (int m = firstPage; m < firstPage + gatherPages; m += firstPageRatio)
                {
                    Console.WriteLine($"pages:{m} \t " + gatherUrl.Replace("$pageno", m.ToString()));
                    GetListOnly(dt, commonParams, DynamicFormDataParamUrl, delayMS, false, classID, listOpcID, provinceID, sourceID, seqNo, keyword, gatherUrl.Replace("$pageno1", (m - 1).ToString()).Replace("$pageno", m.ToString()), listIsPost, listCharset, listPattern, m, urlPattern, listBegin, listEnd, infoCharset, infoRequestHeader, infoOpcID, gatherType, infoFixedFields, infoVarFields, infoParamsFields, listRequestHeaders);
                }

            }
            //按采集源清空全局Cookie
            cookieStringsGlobal = string.Empty;
            //插入到通用列表模板表  t_gather_infopage_model
            DAL.LoadDataTableToDBModelTable(dt, "t_gather_list_model");
        }

        /// <summary>
        /// 信息页面采集主功能函数
        /// </summary>
        /// <param name="dt"></param>
        private static DataTable GatherInfoPage(DataTable dt,out List<string> fieldsForLoadModel)
        {
            string response;
            List<string> fieldsInSingleColumn = new List<string> { "status", "gather_result", "parent_id" };
            DataTable dtNew = dt.Clone();
            int total = dt.Rows.Count;
            for (int i = 0; i < total; i++)
            {
                DataRow dr = dtNew.NewRow();
                Console.WriteLine($"    正在采集\t{i+1}/{total} \t id:{ dt.Rows[i]["id"].ToString()} \t sourceID:{dt.Rows[i]["source_id"].ToString()}\r\n\t{dt.Rows[i]["info_url"].ToString()}");
                response = GetData(dt.Rows[i]["info_url"].ToString(), out bool isException, dt.Rows[i]["info_request_headers"].ToString());

                //访问URL正常返回数据
                if (!isException)
                {
                    string errorStrings = string.Empty;
                    response = DealwithResponse(response);

                    //加载变化数据
                    foreach (var item in dt.Rows[i]["info_var_fields"].ToString().Split(','))
                    {
                        dr[item] = GetInfoPageData(response, dt.Rows[i][item].ToString(), item, out string error);
                        errorStrings += error;
                        if (!fieldsInSingleColumn.Contains(item))
                            fieldsInSingleColumn.Add(item);
                    }
                    //加载固定数据
                    foreach (var item in dt.Rows[i]["info_fixed_fields"].ToString().Split(','))
                    {
                        dr[item] = dt.Rows[i][item];
                        if (!fieldsInSingleColumn.Contains(item))
                            fieldsInSingleColumn.Add(item);
                    }

                    dr["gather_result"] = errorStrings;

                    if (errorStrings.Length > 0)
                    {
                        dr["status"] = 2;   //采集失败
                        dr["parent_id"] = dt.Rows[i]["id"].ToString();
                    }
                    else
                    {
                        dr["status"] = 1;   //采集成功
                        dr["parent_id"] = dt.Rows[i]["id"].ToString();
                    }
                }
                else //访问异常,该列都是日志所需的字段
                {
                    dr["status"] = 2;
                    dr["gather_result"] = response;
                    dr["class_id"] = dt.Rows[i]["class_id"].ToString();
                    dr["source_id"] = dt.Rows[i]["source_id"].ToString();
                    dr["hid"] = dt.Rows[i]["hid"].ToString();
                    dr["info_url"] = dt.Rows[i]["info_url"].ToString();
                    dr["parent_id"] = dt.Rows[i]["id"].ToString();
                }
                dtNew.Rows.Add(dr);
            }
            fieldsForLoadModel = fieldsInSingleColumn;
            return dtNew;
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
                    DataColumn dc = new DataColumn() { ColumnName = matches[i].Groups[1].Value.ToString() };
                    //正则用“|”将数据统一到同一列上的时候，会需要多次用到同一个列，此处只添加一次
                    if (!dt.Columns.Contains(dc.ColumnName))
                    {
                        dt.Columns.Add(dc);
                    }
                }


                //除了正则列和必须的列以外，列表固定列
                dt.Columns.Add("source_id");
                dt.Columns.Add("class_id");
                dt.Columns.Add("list_opc_id");
                dt.Columns.Add("info_opc_id");
                dt.Columns.Add("province_id");
                dt.Columns.Add("gather_type");
                dt.Columns.Add("info_url");
                dt.Columns.Add("keyword");
                dt.Columns.Add("seq_no");

                //除了正则列和必须的列以外，信息固定列
                dt.Columns.Add("info_charset");
                dt.Columns.Add("info_request_headers");
                dt.Columns.Add("info_fixed_fields");
                dt.Columns.Add("info_var_fields");
                dt.Columns.Add("info_params_fields");
            }
            return dt;
        }

        /// <summary>
        /// POST方式获取网址响应
        /// </summary>
        /// <param name="gatherUrl"></param>
        /// <param name="charset"></param>
        /// <param name="isException"></param>
        /// <returns></returns>
        private static string PostData(string gatherUrl, out bool isException,string charset, string requestHeaders = "", string dynamicFormData = "")
        {
            HttpWebRequest myRequest = null; HttpWebResponse myResponse = null;
            string content;
            try
            {
                //数据库网址URL以？号分割, POSTformdata里面也有|，因此不能用split,用最多拆分成2部分的重载
                string url = GetPostUrlString(gatherUrl);
                string postData = GetPostDataString(gatherUrl);

                //WebForm格式的网站，需要迭代获取viewState进行访问
                if (viewStateStringGlobal.Length > 0)
                    postData = postData.Replace("$vs", viewStateStringGlobal).Replace("$Pager", "%24Pager");

                //+https://b2b.10086.cn/b2b/main/listVendorNotice.html?noticeType=2这个网站FormData参数都是动态生成的，结果在源码里面，不采用WebBrowser。
                if (dynamicFormDataGlobal.Length > 0)
                    postData = postData.Replace("$dm", dynamicFormData);

                //避免GetRequestStream()超时
                System.GC.Collect();
                myRequest = (HttpWebRequest)WebRequest.Create(url);
                myRequest.Timeout = 60000;
                myRequest.Method = "POST";
                SetRequestHeadersForPostData(myRequest, requestHeaders, cookieStringsGlobal);

                //将FormData写入HttpWebRequest的访问流中
                Byte[] data = ConvertStringToByteArray(postData);
                Stream newStream = myRequest.GetRequestStream();
                newStream.Write(data, 0, data.Length);
                newStream.Close();
                try
                {
                    content = GetResponseString(myRequest, out isException, charset);
                    isException = false;
                    return content;
                }
                catch (Exception ex)
                {
                    isException = true;
                    return ex.Message;
                }
            }
            catch (Exception e)
            {
                isException = true;
                return e.Message;
            }
            finally
            {
                //取消访问，关闭响应，避免超时
                if (myResponse != null)
                    myResponse.Close();
                if (myRequest != null)
                    myRequest.Abort();
            }
        }

        /// <summary>
        /// GET方式获取网址响应
        /// </summary>
        /// <param name="url"></param>
        /// <param name="charset"></param>
        /// <param name="isException"></param>
        /// <returns></returns>
        private static string GetData(string url, out bool isException,string charset, string requestHeaders = "")
        {
            HttpWebRequest myRequest = null; HttpWebResponse myResponse = null;
            string responseText;
            try
            {
                Util.SetCertificatePolicy();//验证安全，未能为 SSL/TLS 安全通道建立信任关系
                System.GC.Collect();//避免GetRequestStream()超时
                myRequest = (HttpWebRequest)WebRequest.Create(url);
                myRequest.Timeout = 60000;
                myRequest.Method = "GET";
                SetRequestHeadersForGetData(myRequest, requestHeaders, cookieStringsGlobal);
                try
                {
                    responseText = GetResponseString(myRequest, out isException, charset);
                    return responseText;
                }
                catch (Exception e)
                {
                    isException = true;
                    return e.Message.ToString();
                }
            }
            catch (Exception e)
            {
                isException = true;
                return e.Message.ToString();
            }
            finally
            {
                //避免超时,释放资源
                if (myResponse != null)
                    myResponse.Close();
                if (myRequest != null)
                    myRequest.Abort();
            }
        }

        /// <summary>
        /// 获取列表上的数据主函数
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="sourceid"></param>
        /// <param name="gatherUrl"></param>
        /// <param name="ispost"></param>
        /// <param name="listCharset"></param>
        /// <param name="pattern"></param>
        /// <param name="pageNo"></param>
        /// <param name="infoUrl"></param>
        /// <param name="urlPattern"></param>
        /// <param name="listBegin"></param>
        /// <param name="listEnd"></param>
        /// <returns></returns>
        private static DataTable GetListOnly(DataTable dt, int commonParams, string DynamicFormDataParamUrl, int delayMS, bool isViewState, int classID, int listOpcID, int provinceID, int sourceID, int seqNo, string keyword, string gatherUrl, bool listIsPost, string listCharset, string listPattern, int pageNo,string urlPattern, string listBegin, string listEnd, string infoCharset, string infoRequestHeader, int infoOpcID, int gatherType, string infoFixedFields, string infoVarFields, string infoParamsFields, string listRequestHeaders)
        {

            #region 1.采集过程
            //直接获取会没有响应，先延迟下
            if (delayMS > 0)
            {
                Console.WriteLine($"\t\t\t列表参数设定，强制休眠{delayMS}毫秒...");
                Thread.Sleep(delayMS);
            }

            //获取JS页面计算结果，不管其算法过程，因为结果是固定的
            gatherUrl = GetJavaScriptResult(sourceID, gatherUrl, pageNo);

            //采集开始时间
            string gatherBT = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            bool isRegMatch = false;//是否匹配到数据
            bool isException;//HTTP获取数据是否异常
            string response;//获取响应内容或者异常信息
            int gatherRows = 0;//正则匹配到的条数基本等于每个列表页面的链接数
            string url = string.Empty, url1 = string.Empty, url2 = string.Empty;
            string url3 = string.Empty, url4 = string.Empty, pd = string.Empty;//构成infoUrl的6个参数，替代数据库里面的$!,$2...



            if ((commonParams & 1) == 1)//用WebBrowser来搞定输出，效率低，最后的办法
            {
                response = GetDataByWebBrowser(gatherUrl, out isException);
            }
            else if (isViewState)//是否是WebForm程序
            {
                viewStateStringGlobal = "使用GET获取VIEWSTATE";
                response = GetData(gatherUrl, out isException, listCharset, listRequestHeaders);
            }
            else if (DynamicFormDataParamUrl.Length > 0 && dynamicFormDataGlobal.Length == 0)//动态FormData是否已经获取，放在这里目的是:有可能不需要获取Cookie，直接访问动态表格参数网址
            {
                response = GetData(DynamicFormDataParamUrl, out isException, listCharset, listRequestHeaders);
                dynamicFormDataGlobal = GetDynamicFormDataParam(response, sourceID);
            }
            else if (listIsPost)
                response = PostData(gatherUrl, out isException, listCharset, listRequestHeaders, dynamicFormDataGlobal);
            else
            {
                response = GetData(gatherUrl, out isException, listCharset,listRequestHeaders);
            }
            //没有异常
            try
            {
                if (!isException)
                {
                    response = DealwithResponse(response);
                    Regex rg = new Regex(listPattern, RegexOptions.IgnoreCase);//指定不区分大小写的匹配
                    if (response != null || response.Length > 0)
                    {
                        if (listBegin.Length > 0 && listEnd.Length > 0)
                            response = SubstringResponse(response, listBegin, listEnd);

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
                                if (dt.Columns.Contains("info_title"))
                                {
                                    dr["info_title"] = DealWithTitle(dr["info_title"].ToString());
                                }

                                //防止日期采集出问题,
                                if (dt.Columns.Contains("publish_date"))
                                {
                                    pd = DealWithPublishDate(dr["publish_date"].ToString(), gatherUrl);
                                    dr["publish_date"] = pd;
                                }
                                if (dt.Columns.Contains("url"))
                                {
                                    url = dr["url"].ToString();
                                    dr["url"] = null;
                                }
                                if (dt.Columns.Contains("url1"))
                                {
                                    url1 = dr["url1"].ToString();
                                    dr["url1"] = null;
                                }
                                if (dt.Columns.Contains("url2"))
                                {
                                    url2 = dr["url2"].ToString();
                                    dr["url2"] = null;
                                }
                                if (dt.Columns.Contains("url3"))
                                {
                                    url3 = dr["url3"].ToString();
                                    dr["url3"] = null;
                                }
                                if (dt.Columns.Contains("url4"))
                                {
                                    url4 = dr["url4"].ToString();
                                    dr["url4"] = null;
                                }

                                //凭借为完整的可访问的网址
                                dr["info_url"] = DealWithUrlDirectoryAndFormmater(sourceID, urlPattern, url, url1, url2, url3, url4, pd);

                                //固定必须有的数据（采集源）
                                dr["source_id"] = sourceID.ToString();
                                dr["class_id"] = classID.ToString();
                                dr["list_opc_id"] = listOpcID.ToString();
                                dr["info_opc_id"] = infoOpcID.ToString();
                                dr["province_id"] = provinceID.ToString();
                                dr["keyword"] = UrlDecode(keyword, Encoding.GetEncoding(listCharset));
                                dr["seq_no"] = seqNo.ToString();
                                dr["info_charset"] = infoCharset;
                                dr["info_request_headers"] = infoRequestHeader;
                                dr["gather_type"] = gatherType.ToString();
                                dr["info_fixed_fields"] = infoFixedFields;
                                dr["info_var_fields"] = infoVarFields;
                                dr["info_params_fields"] = infoParamsFields;
                                dt.Rows.Add(dr);
                            }
                        }
                    }

                }
            }
            catch
            {
                throw;
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
            int ok = DAL.WriteLog(classID, sourceID, seqNo, UrlDecode(keyword, Encoding.GetEncoding(listCharset)), pageNo, isRegMatch, isException, response, listPattern, gatherBT, gatherET, gatherRows);
            while (ok != 1 && cnt < 3)
            {
                Console.WriteLine($" 第{cnt}次 重试写入采集日志表...");
                ok = DAL.WriteLog(classID, sourceID, seqNo, UrlDecode(keyword, Encoding.GetEncoding(listCharset)), pageNo, isRegMatch, isException, response, listPattern, gatherBT, gatherET, gatherRows);
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
                case "now-yyyy-mm-dd-tomorrow": _result = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"); break;
                case "now-yyyy-mm-dd-ts0": _result = ConvertDateTimeToTimeStamp(DateTime.Now, 0); break;
                case "now-yyyy-mm-dd-ts3": _result = ConvertDateTimeToTimeStamp(DateTime.Now, 3); break;
                case "now-yyyy-mm-dd-ts7": _result = ConvertDateTimeToTimeStamp(DateTime.Now, 7); break;
                default:
                    break;
            }
            return _result;
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

            //Regex rg = new Regex(listbegin + ".*?" + listend);
            //return rg.Match(content).ToString();
            return Regex.Match(content, listbegin + "(.*?)" + listend, RegexOptions.IgnoreCase).Groups[1].Value;
        }

        /// <summary>
        /// 原始内容处理（去空值和脚本内容）
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static string DealwithResponse(string content)
        {
            string result;
            //.Replace("&nbsp;", "")不能替换掉空格，否则时间可能异常
            content = content.Replace(@"\t", "").Replace(@"\n", "").Replace(@"\r", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

            //将内容中的脚本去除
            content = Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(content
                , @"<script.*?</script>", "", RegexOptions.IgnoreCase)
                , "<!--.*?-->", "", RegexOptions.IgnoreCase)
                , "<style.*?</style>", "", RegexOptions.IgnoreCase)
                , @"replace.*?\(.*?\)", "", RegexOptions.IgnoreCase)
                , "var.*?/.*?/", "", RegexOptions.IgnoreCase);

            //content = Regex.Replace(content, @"<script.*?</script>", "", RegexOptions.IgnoreCase);
            //content = Regex.Replace(content, @"replace.*?\(.*?\)", "", RegexOptions.IgnoreCase);
            //content = Regex.Replace(content, @"var.*?/.*?/", "", RegexOptions.IgnoreCase);
            //content = Regex.Replace(content, @"<!--.*?-->", "", RegexOptions.IgnoreCase);
            //content = Regex.Replace(content, @"<style.*?</style>", "", RegexOptions.IgnoreCase);
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
        /// 处理空白符和后台源代码中的空格
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string DealWithBlank(string input)
        {
            if (input != null)
            {
                return input.Replace("\t", "").Replace("\r", "").Replace("\n", "").Replace(" ", "");
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

            string result = DealWithBlank(Regex.Replace(title, "</?[a-z].*?>", "", RegexOptions.IgnoreCase).Replace("&nbsp;", ""));
            if (result.Length > 200)
            {
                return result.Substring(0, 200);//限制在200个字符以内
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// 处理信息页面采集到的内容格式
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static string DealWithContent(string content)
        {
            //return Regex.Replace(content, "</?[a-z].*?>", "", RegexOptions.IgnoreCase);  保留格式，不保留字体。
            return Regex.Replace(Regex.Replace(content, "<!--.*?-->", "", RegexOptions.IgnoreCase), "</?font.*?>", "", RegexOptions.IgnoreCase);
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
            string _publishDate = Regex.Replace(Regex.Replace(publishDate, "<a.*?</a>", "", RegexOptions.IgnoreCase), "<.*?>", "")
                                        .Replace("年", "-").Replace("月", "-").Replace("日", "")
                                         .Replace("[", "").Replace("]", "").Replace("【", "").Replace("】", "")
                                         .Replace("（", "").Replace("(", "").Replace("）", "").Replace(")", "").Replace("&nbsp;", " ");
            if (_publishDate.Length == 0) //如果为空，则设置为明天
            {
                return DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            }
            else if (DateTime.TryParse(_publishDate, out DateTime _result)) //可以转换为时间
            {
                return _result.ToString("yyyy-MM-dd");
            }
            else if (Int64.TryParse(_publishDate, out _)) //整数时间戳的情况下，转换为符合日期的字符串
            {
                return ConvertTimeStampToDateTime(_publishDate).ToString("yyyy-MM-dd HH:mm:ss");

            }
            else
            {
                if (url.StartsWith("http://www.ccgp-sichuan.gov.cn"))//特殊格式的日期，处理为正常值
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
                else if (url.StartsWith("http://www.ccgp-tianjin.gov.cn"))
                {
                    return DateTime.ParseExact(_publishDate, "ddd MMM dd HH:mm:ss CST yyyy", new CultureInfo("en-us")).ToString("yyyy-MM-dd");
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
        private static string DealWithUrlDirectoryAndFormmater(int sourceID, string urlpattern, string url, string url1, string url2, string url3, string url4, string publishDate)
        {
            string CorrectUrl;
            if (url.StartsWith("."))
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
                    CorrectUrl = domain + url.Replace("../", "");


                }
                else //if (url.StartsWith("./"))
                {
                    CorrectUrl = urlpattern + url.Replace("./", "");
                }
            }
            else if (urlpattern.IndexOf("$1") > -1)
            {
                switch (sourceID)
                {
                    case 52: CorrectUrl = urlpattern.Replace("$1", url.Substring(0, 6)).Replace("$2", url.Substring(0, 9)).Replace("$3", url).Replace("$4", publishDate.Replace("-", "")).Replace("$5", url1); break;
                    case 53: CorrectUrl = urlpattern.Replace("$1", url.Substring(0, 6)).Replace("$2", url.Substring(0, 9)).Replace("$3", url).Replace("$4", publishDate.Replace("-", "")).Replace("$5", url1); break;
                    case 855: CorrectUrl = urlpattern.Replace("$1", publishDate.Replace("-", "")).Replace("$2", url); break;
                    case 858: CorrectUrl = urlpattern.Replace("$1", publishDate.Replace("-", "")).Replace("$2", url); break;
                    case 859: CorrectUrl = urlpattern.Replace("$1", publishDate.Replace("-", "")).Replace("$2", url); break;
                    case 860: CorrectUrl = urlpattern.Replace("$1", publishDate.Replace("-", "")).Replace("$2", url); break;
                    case 403: CorrectUrl = urlpattern.Replace("$1", url == "014001001" ? "xqfzx" : "jygk").Replace("$2", url.Substring(0, 6)).Replace("$3", url).Replace("$4", publishDate.Replace("-", "")).Replace("$5", url1); break;
                    case 875: CorrectUrl = urlpattern.Replace("$1", url == "014002001" ? "xqfzx" : "jygk").Replace("$2", url.Substring(0, 6)).Replace("$3", url).Replace("$4", publishDate.Replace("-", "")).Replace("$5", url1); break;
                    default: CorrectUrl = "info_url中使用了$1,$2等需要计算的参数，必须在程序里面添加特例..."; break;
                }
            }
            else
            {
                CorrectUrl = urlpattern.Replace("$url4", url4).Replace("$url3", url3).Replace("$url2", url2).Replace("$url1", url1).Replace("$url", url);
            }
            //链接不能替换Replace("//", "/")
            return System.Web.HttpUtility.HtmlDecode(CorrectUrl);
        }

        /// <summary>
        /// 获取采集列表类型查询语句         
        /// </summary>
        /// <param name="autoRun"></param>
        /// <returns></returns>
        private static string OnGetItemSourceListString()
        {
            if (gatherSpeed == 0)
                return ifDESC ? @"Select *  from t_item_source_list where status=1  order by id desc " : @"Select *  from t_item_source_list where status=1  ";
            else if (gatherSpeed == 1)
                return ifDESC ? @"Select *  from t_item_source_list a join t_gather_list_access_level_last b on a.id=b.source_id where status=1 and b.current_level=1 order by id desc  " : @"Select *  from t_item_source_list a join t_gather_list_access_level_last b on a.id=b.source_id where status=1 and b.current_level=1  ";
            else
                return ifDESC ? @"Select *  from t_item_source_list a join t_gather_list_access_level_last b on a.id=b.source_id where status=1 and b.current_level=2 order by id desc " : @"Select *  from t_item_source_list a join t_gather_list_access_level_last b on a.id=b.source_id where status=1 and b.current_level=2  ";
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

        /// <summary>
        /// 获取具体信息页面采集关键字查询语句
        /// </summary>
        /// <returns></returns>
        private static string OnGetInfoPageWaitingString()
        {
            return ifDESC ? @"select * from t_gather_infopage_waiting where status = 0 and ins_time> cast(dateadd(dd, -7, getdate()) as date) order by id desc  " : @"select * from t_gather_infopage_waiting where status = 0 and ins_time> cast(dateadd(dd, -7, getdate()) as date) ";
            //ValidateInput("debug", queryString);

        }

        /// <summary>
        /// 初始化采集的类型以及提取DataSet的查询语句
        /// </summary>
        /// <param name="gatherType"></param>
        /// <returns></returns>
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
            bool legal = false; string condition;
            string input, message;
            string queryString1 = string.Empty, queryString2 = string.Empty, queryString3 = string.Empty;
            if (gatherType == "1")
            {
                message = "\n\n\n=====  请输入列表采集源ID，多个值用 < 逗号 > 分隔， 数字区间用 < 空格 > 分隔  ，【ENTER键】 确认";
                queryString1 = OnGetItemSourceListString();
                queryString2 = OnGetSearchKeywordListString();
            }
            else
            {
                message = "\n=====  请输入待采信息Waiting表信息ID，多个值用 < 逗号 > 分隔， 数字区间用 < 空格 > 分隔  ，  【ENTER键】 确认";
                queryString3 = OnGetInfoPageWaitingString();
            }

            Console.WriteLine(message);
            do
            {
                input = Console.ReadLine().Replace("，", ",");
                input = input.EndsWith(",", StringComparison.OrdinalIgnoreCase) ? input.Substring(0, input.Length - 1) : input;

                if (input.Contains(" "))
                {
                    foreach (var item in input.Split(new char[] { ' ' }, 2))
                    {
                        legal = int.TryParse(item, out _);
                        if (!legal)
                        {
                            Console.WriteLine("\t   ~888~ 请输入正确合法的数字ID ...  ");
                            break;
                        }
                    }
                    condition = " and id between " + input.Split(new char[] { ' ' }, 2)[0] + " and " + input.Split(new char[] { ' ' }, 2)[1];
                }
                else
                {
                    foreach (var item in input.Split(new string[] { "," }, StringSplitOptions.None))
                    {
                        legal = int.TryParse(item, out _);
                        if (!legal)
                        {
                            Console.WriteLine("\t   ~888~ 请输入正确合法的数字ID ...  ");
                            break;
                        }

                    }
                    condition = " and id in(" + input + ");  ";
                }

            }
            while (!legal);
            Console.WriteLine("\n");
            queryString1 = string.IsNullOrEmpty(queryString1) ? "" : queryString1 + condition;
            queryString2 = string.IsNullOrEmpty(queryString2) ? "" : queryString2 + condition;
            queryString3 = string.IsNullOrEmpty(queryString3) ? "" : queryString3 + condition;
            return queryString1 + queryString2 + queryString3;
        }

        /// <summary>
        /// 控制台类型输入验证
        /// </summary>
        /// <returns></returns>
        private static string ValidateInputGatherType()
        {
            string input;
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
        private static string UrlEncode(string keyword, Encoding charset)
        {
            return HttpUtility.UrlEncode(keyword, charset);
        }

        /// <summary>
        /// 百分号格式转中文
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        private static string UrlDecode(string keyword, Encoding charset)
        {
            return HttpUtility.UrlDecode(keyword, charset);

        }

        /// <summary>
        /// 将数字格式的时间戳转换为日期格式
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        private static DateTime ConvertTimeStampToDateTime(string timeStamp)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(Regex.Replace(timeStamp, @"\s", "") + "0000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }

        /// <summary>
        /// 将日期转换为N为小数的时间戳
        /// </summary>
        /// <param name="time"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private static string ConvertDateTimeToTimeStamp(DateTime time, int scale = 0)
        {
            TimeSpan cha = (time - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)));
            double t = (double)cha.TotalSeconds;
            return ((long)(Math.Round(t, scale) * Math.Pow(10, scale))).ToString();
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
                return DealWithPublishDate(text, "") ?? DateTime.Now.ToString("yyyy-MM-dd");
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
            string begin;string end;string r;
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
            if (field == "publish_date")
            {
                outErrorFiels = null;
                return DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                outErrorFiels = field + " -- 正则匹配失败   ";
                return pattern;
            }

        }

        /// <summary>
        /// 根据首页链接获取网站的cookie,然后post访问,暂时没有用
        /// </summary>
        /// <param name="cookieUrl"></param>
        /// <returns></returns>
        private static void GetCookieBySourceUrl(string cookieUrl, string requestHeaders, string DynamicFormDataParamUrl, int sourceID,string charSet)
        {
            string content = string.Empty;
            string cookiesstr = string.Empty;
            HttpWebRequest myRequest = null;
            HttpWebResponse myResponse = null;
            string gatherUrl;
            try
            {
                if (cookieUrl.StartsWith("++"))
                {
                    gatherUrl = cookieUrl.Replace("++", "").Replace(" ", "");
                    string postData = gatherUrl.Split(new char[] { '|' }, 2)[1];
                    string url = gatherUrl.Split(new char[] { '|' }, 2)[0];

                    ASCIIEncoding encoding = new ASCIIEncoding();
                    byte[] data = encoding.GetBytes(postData);
                    myRequest = (HttpWebRequest)WebRequest.Create(url);
                    myRequest.Method = "Post";
                    Stream newStream = myRequest.GetRequestStream();
                    newStream.Write(data, 0, data.Length);
                    newStream.Close();
                    myRequest.AllowAutoRedirect = false;
                    myRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.116 Safari/537.36";
                    myRequest.ContentType = "application/x-www-form-urlencoded";
                    AddRequestHeaders(myRequest, requestHeaders);//先添加其他header并且清除已有的Cookie
                    HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                    myRequest.CachePolicy = noCachePolicy;
                    if (cookiesstr.Length > 0)//添加最新产生的Cookie
                    {
                        myRequest.Headers.Add("Cookie", cookiesstr);
                    }
                    try
                    {
                        myResponse = (HttpWebResponse)myRequest.GetResponse();
                    }
                    catch (Exception e)
                    {
                        DAL.WriteLog(0, 0, 0, "", 0, false, true, e.Message, cookieUrl, DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), 0);
                        return;
                    }
                    //cookiesHostHolder.Add(host);
                    return;
                    //return cc;
                }
                else if (cookieUrl.StartsWith("+"))
                {

                    foreach (var item in cookieUrl.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        myRequest = (HttpWebRequest)WebRequest.Create(item);
                        myRequest.Method = "GET";
                        myRequest.AllowAutoRedirect = false;//获取cookie不允许重定向，否则中间会丢失一个
                        //AddRequestHeaders(myRequest, requestHeaders);//先添加其他header并且清除已有的Cookie,首页访问一般不会带Header,
                        myRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.116 Safari/537.36";

                        if (cookiesstr.Length > 0)//添加最新产生的Cookie
                            myRequest.Headers.Add("Cookie", cookiesstr);
                        try
                        {
                            try
                            {
                                myResponse = (HttpWebResponse)myRequest.GetResponse();
                            }
                            catch (WebException ex)
                            {
                                myResponse = (HttpWebResponse)ex.Response;
                            }

                            if (myResponse.Headers.Get("Set-Cookie") != null)
                            {   //可能一次有多个Cookie返回
                                string allCookies = myResponse.Headers.Get("Set-Cookie");
                                foreach (string cook in allCookies.Split(new char[] { ',', '，' }))
                                {
                                    cookiesstr += ';' + cook.Split(new char[] { ';', '；' })[0];
                                }
                                cookiesstr = cookiesstr.Substring(1);
                            }


                            //正常情况下，只是为了获取响应的cookie，不获取响应内容，如果填写了动态参数获取，才用StreamReader数据
                            if (DynamicFormDataParamUrl.Length > 0)
                            {
                                if (myResponse.StatusCode == HttpStatusCode.OK && myResponse.ContentLength < 1024 * 1024)
                                    content = GetDecodedTextFromResponse(myResponse, charSet);
                                dynamicFormDataGlobal=GetDynamicFormDataParam(content, sourceID);
                            }
                        }
                        catch (Exception e)
                        {
                            DAL.WriteLog(0, 0, 0, "", 0, false, true, e.Message, cookieUrl, DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), 0);
                            return;
                        }
                    }
                    //cookiesHostHolder.Add(host);
                    cookieStringsGlobal += cookiesstr;
                }
                else
                    return;
            }
            catch (Exception e)
            {
                DAL.WriteLog(0, 0, 0, "", 0, false, true, e.Message, cookieUrl, DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), 0);
            }
            finally
            {
                if (myResponse != null)
                    myResponse.Close();
                if (myRequest != null)
                    myRequest.Abort();
            }
        }

        /// <summary>
        /// 根据t_item_source_list表的list_request_headers 和 info_request_headers 内容添加到HttpRequest.Headers
        /// </summary>
        /// <param name="request"></param>
        /// <param name="requestHeaders"></param>
        public static void AddRequestHeaders(HttpWebRequest request, string requestHeaders)
        {
            //&& request.CookieContainer.GetCookies(request.RequestUri) == null
            try
            {   //将头部格式不改变的加入  不需要替换中间的-； Content-Type  √    ContentType ×
                if (requestHeaders.Length > 0)
                {
                    foreach (var item in requestHeaders.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        SetHeaderValue(request.Headers, item.Split(new char[] { ':', '：' }, 2)[0], item.Split(new char[] { ':', '：' }, 2)[1]);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("requestHeader格式错误，保留回车换行", e);

            }


        }

        /// <summary>
        /// 将字串形式的Headers，添加到HttpRequest.Headers对象中
        /// </summary>
        /// <param name="header"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void SetHeaderValue(WebHeaderCollection header, string name, string value)
        {
            var property = typeof(WebHeaderCollection).GetProperty("InnerCollection", BindingFlags.Instance | BindingFlags.NonPublic);
            if (property != null)
            {
                var collection = property.GetValue(header, null) as NameValueCollection;
                collection[name] = value;
            }

        }

        /// <summary>
        /// 获取页面参数==固定JS算法结果
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="url"></param>
        /// <param name="pageno"></param>
        /// <returns></returns>
        private static string GetJavaScriptResult(int sourceId, string url, int pageno)
        {
            if (sourceId == 305)
            {
                switch (pageno)
                {
                    case 1: url = url.Replace("$js", "56fa055b48ba42e320bad7fed54ccc23"); break;
                    case 2: url = url.Replace("$js", "6b79cbac4c0f9cc3a8dc62290eb96274"); break;
                    case 3: url = url.Replace("$js", "48ccbf74bf755213f942623554c3e3a9"); break;
                    case 4: url = url.Replace("$js", "6343da676f1d1659e32dab239571d2cd"); break;
                    case 5: url = url.Replace("$js", "09b206b85700b84c180eb36a15f6390c"); break;
                    case 6: url = url.Replace("$js", "c13faab34605c3c1a94d7b311cf16cc7"); break;
                    case 7: url = url.Replace("$js", "a33c9f16084ec87b69af371a38db5ddb"); break;
                    case 8: url = url.Replace("$js", "36c53bc708ec3bd9a4d7d10a1136a735"); break;
                    case 9: url = url.Replace("$js", "8866e6a67ea822b1543142c9c03237fb"); break;
                    case 10: url = url.Replace("$js", "2bfc7a8f88d05e7cfb51da395716d5ac"); break;
                    default:
                        break;
                }
            }
            return url;
        }

        /// <summary>
        /// 获取页面的__VIEWSTATE，__VIEWSTATEGENERATOR，__VIEWSTATEENCRYPTED相关的参数
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static string GetViewStateParametersStringFormmater(string response)
        {
            return "__VIEWSTATE=" + HttpUtility.UrlEncode(Regex.Match(response, "id=\"__VIEWSTATE\"[^\"]*\"([^\"]*)").Groups[1].Value)
            //+"&__EVENTVALIDATION=" + Regex.Match(response, "id=\"__EVENTVALIDATION\"[^\"]*\"([^\"]*)").Groups[1].Value
            + "&__VIEWSTATEGENERATOR=" + Regex.Match(response, "id=\"__VIEWSTATEGENERATOR\"[^\"]*\"([^\"]*)").Groups[1].Value
            + "&__VIEWSTATEENCRYPTED=" + Regex.Match(response, "id=\"__VIEWSTATEENCRYPTED\"[^\"]*\"([^\"]*)").Groups[1].Value
            // +"&__EVENTTARGET=" + Regex.Match(response, "id=\"__EVENTTARGET\"[^\"]*\"([^\"]*)").Groups[1].Value
            //+ "&__EVENTARGUMENT=" + Regex.Match(response, "id=\"__EVENTARGUMENT\"[^\"]*\"([^\"]*)").Groups[1].Value
            ;
        }

        /// <summary>
        /// 获取访问首页的源码数据，拼接为formdata访问，此参数为动态的
        /// </summary>
        /// <param name="response"></param>
        /// <param name="sourceID"></param>
        private static string GetDynamicFormDataParam(string response, int sourceID)
        {
            string result = string.Empty;
            if (sourceID == 72)
                result = "_qt=" + HttpUtility.UrlEncode(Regex.Match(response, "name=\"_qt\"[^\"]*\"([^\"]*)").Groups[1].Value);
            return result;
        }

        /// <summary>
        /// 利用WebBrowser获取页面的响应文本,这个不适合异步数据？？？区分不了哪个XHR是需要的？？？
        /// 或者加载完成后，用界面操作文档获取列表的元素（不同的网页又需要不同的获取方法，很是麻烦，绝大部分采取之前的HttpWebRequest方法）
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <returns></returns>
        private static string GetDataByWebBrowser(string sourceUrl, out bool isException, bool isPost = false, string additonalHeaders = "")
        {

            //使用WebBrowser必须加WinFrom环境
            //System.Windows.Forms.Application.EnableVisualStyles();
            //System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            WebBrowser wb = new WebBrowser();
            try
            {
                wb.DocumentCompleted += WbDocumentCompleted;  //新增事件处理程序
                wb.ScriptErrorsSuppressed = false;  //不显示错误消息程序
                if (isPost)//Post
                {
                    string Url = GetPostUrlString(sourceUrl);
                    byte[] postData = ConvertStringToByteArray(GetPostDataString(sourceUrl));
                    //还没有用的到addtionalHeaders,这本身就是个浏览器了，最多附加的也就是cookieStrings了吧？？？
                    //additonalHeaders = additonalHeaders.Length > 0 ? additonalHeaders : basicPostRequestHeaders;
                    //additonalHeaders += basicPostRequestHeaders;
                    wb.Navigate(Url, "", postData, additonalHeaders);
                }
                else //Get
                {
                    wb.Navigate(sourceUrl);
                }

                //若没加载完则继续加载
                while (wb.ReadyState < WebBrowserReadyState.Complete)
                    System.Windows.Forms.Application.DoEvents();

                //获取数据流，转换为文本数据
                Encoding encoding = Encoding.GetEncoding(wb.Document.Encoding);
                StreamReader stream = new StreamReader(wb.DocumentStream, encoding);
                string content = stream.ReadToEnd();
                isException = false;
                return content;
            }
            catch (Exception ex)
            {
                isException = true;
                return ex.Message;
            }
            finally
            {
                wb.Dispose();
                //GC.Collect(); 暂时不调用
                //GC.WaitForPendingFinalizers();
            }

        }

        /// <summary>
        /// WebBrowser加载完毕事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void WbDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (e.Url.ToString().Contains("chinaunicombidding"))
            {
                //完成后重新加载当前显示在WebBrowser里面的文档
                ((WebBrowser)sender).Refresh();
            }
        }

        /// <summary>
        /// 获取HttpWebResponse中获取的字符集，将其Stream编译为对应的编码response.CharacterSet
        /// </summary>
        /// <param name="responseCharSet"></param>
        /// <returns></returns>
        private static Encoding GetResponseEncoding(string responseCharSet)
        {
            Encoding encoding;
            switch (responseCharSet.ToLower())           //小写
            {
                case "gbk":
                    encoding = Encoding.GetEncoding("GBK");
                    break;
                case "gb2312":
                    encoding = Encoding.GetEncoding("GB2312");
                    break;
                case "utf-8":
                    encoding = Encoding.UTF8;
                    break;
                case "iso-8859-1":
                    encoding = Encoding.GetEncoding("GBK"); //GB2312              
                    break;
                case "big5":
                    encoding = Encoding.GetEncoding("Big5");
                    break;
                default:
                    encoding = Encoding.UTF8;
                    break; 
            }
            return encoding;
        }

        /// <summary>
        /// 获取GatherUrl中的Url部分
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <returns></returns>
        private static string GetPostUrlString(string sourceUrl)
        {
            return sourceUrl.Split(new char[] { '|' }, 2)[0];
        }

        /// <summary>
        /// 获取GatherUrl中的postData部分
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <returns></returns>
        private static string GetPostDataString(string sourceUrl)
        {
            return sourceUrl.Split(new char[] { '|' }, 2)[1];
        }

        /// <summary>
        /// 转换FormData数据为字节数组
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        private static byte[] ConvertStringToByteArray(string postData)
        {
            ASCIIEncoding encoding = new ASCIIEncoding();
            return encoding.GetBytes(postData);
        }

        /// <summary>
        /// 获取响应文本字符串，返回是否异常
        /// </summary>
        /// <param name="myRequest"></param>
        /// <param name="isException"></param>
        /// <returns></returns>
        private static string GetResponseString(HttpWebRequest myRequest, out bool isException,string charset)
        {
            string responseText;
            HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
            if (myResponse.StatusCode == HttpStatusCode.OK && myResponse.ContentLength < 1024 * 1024)
            {
                responseText = GetDecodedTextFromResponse(myResponse,charset);
                isException = false;

                //获取响应__VIEWSTATE
                if (viewStateStringGlobal.Length > 0)
                    viewStateStringGlobal = GetViewStateParametersStringFormmater(responseText);
            }
            else
            {
                isException = true;
                responseText = $"访问状态为:{myResponse.StatusCode}，请检查是否有重定向等";
            }
            return responseText;
        }

        /// <summary>
        /// 将响应读取为文本数据，以填写的charset为主
        /// </summary>
        /// <param name="myResponse"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        private static string GetDecodedTextFromResponse(HttpWebResponse myResponse,string charset)
        {
            StreamReader reader; string responseText; Encoding encoding;
            try
            {
                encoding = Encoding.GetEncoding(charset);
            }
            catch
            {
                encoding=GetResponseEncoding(myResponse.CharacterSet);
            }

            if (myResponse.ContentEncoding != null && myResponse.ContentEncoding.Equals("gzip", StringComparison.InvariantCultureIgnoreCase))
                reader = new StreamReader(new GZipStream(myResponse.GetResponseStream(), CompressionMode.Decompress), encoding);
            else
                reader = new StreamReader(myResponse.GetResponseStream(),encoding);
            responseText = reader.ReadToEnd();
            reader.Close();
            return responseText;
        }
        /// <summary>
        /// 设置GetData方法的Headers
        /// </summary>
        /// <param name="myRequest"></param>
        /// <param name="requestHeaders"></param>
        /// <param name="cookieStrings"></param>
        private static void SetRequestHeadersForGetData(HttpWebRequest myRequest, string requestHeaders, string cookieStrings)
        {
            SetRequestHeadersForCommon(myRequest, requestHeaders, cookieStrings);

        }

        /// <summary>
        /// 设置PostData方法的Headers
        /// </summary>
        /// <param name="myRequest"></param>
        /// <param name="requestHeaders"></param>
        /// <param name="cookieStrings"></param>
        private static void SetRequestHeadersForPostData(HttpWebRequest myRequest, string requestHeaders, string cookieStrings)
        {
            SetRequestHeadersForCommon(myRequest, requestHeaders, cookieStrings);
            myRequest.ServicePoint.Expect100Continue = false;//卡主不动无法提交
            if (myRequest.Headers["Content-Type"] == null)
            {
                myRequest.ContentType = "application/x-www-form-urlencoded";
            }
        }

        /// <summary>
        /// 设置GetData和PostData的通用Headers
        /// </summary>
        /// <param name="myRequest"></param>
        /// <param name="requestHeaders"></param>
        /// <param name="cookieStrings"></param>
        private static void SetRequestHeadersForCommon(HttpWebRequest myRequest, string requestHeaders, string cookieStrings)
        {

            AddRequestHeaders(myRequest, requestHeaders);
            //强制取消缓存，同浏览器Disable Cache
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            myRequest.CachePolicy = noCachePolicy;
            myRequest.AllowAutoRedirect = true;
            myRequest.Proxy = null;
            myRequest.UseDefaultCredentials = true;
            //多线程超时解决办法。对于前面多个request。其都是keepalive为true，以及多个response也没有close
            ServicePointManager.DefaultConnectionLimit = 10;
            //如果没有填写头部连接属性则默认关闭【基础连接已经关闭: 服务器关闭了本应保持活动状态的连接。】
            if (myRequest.Headers["Connection"] == null)
            {
                myRequest.KeepAlive = false;
            }

            if (myRequest.Headers["User-Agent"] == null)
            {
                myRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.116 Safari/537.36";
            }

            if (cookieStrings.Length > 0)
            {
                if (myRequest.Headers["Cookie"] != null)
                {
                    myRequest.Headers.Remove("Cookie");
                }
                myRequest.Headers.Add("Cookie", cookieStrings);
            }

        }


    }
}