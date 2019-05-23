using System;
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
        //程序主函数：程序开始到程序结束
        static void Main(string[] args)
        {
            DataSet ds; string queryString;
            int callCount = 1;
            string sourceType = ConfigurationManager.AppSettings["sourceType"];
            string autoRun = ConfigurationManager.AppSettings["autoRun"];
            if (sourceType == "1")
            {
                Console.WriteLine("\n\t\t***目前采集的是: { 列表 } 采集源表数据***\n\n");
            }
            else if (sourceType == "2")
            {
                Console.WriteLine("\n\t\t***目前采集的是: { 关键字 } 采集源表数据***\n\n");
            }
            else
            {
                Console.WriteLine($"\n***目前采集的是所有采集源表数据，XXX 目前没有用");
            }

            if (sourceType == "1")
            {
                queryString = OnGetItemSourceList(autoRun);
            }
            else
            {
                queryString = OnGetItemSourceKeyword(autoRun);
            }

            ds = DAL.getAllItemSource(queryString);

            getItemSourceMaster(ds);


            //同步到信息表,监控时间

            Stopwatch sw = new Stopwatch();
            sw.Start(); //开启计时器

            bool ok = DAL.execProcedureNonParamerters("pr_sync_gather_list_model");
            while (!ok && callCount < 3)
            {
                Console.WriteLine("调用同步过程失败时间：" + string.Format("{0} ms", sw.ElapsedMilliseconds) + $" 重复第 {callCount} 次调用...");
                callCount++;

                ok = DAL.execProcedureNonParamerters("pr_sync_gather_list_model");
            }
            sw.Stop(); //停止计时器

            if (ok)
            {
                Console.WriteLine("同步数据执行总时间：" + string.Format("{0} ms", sw.ElapsedMilliseconds) + "\n\r采集完成");
            }
            else
            {
                Console.WriteLine("同步数据执行总时间：" + string.Format("{0} ms", sw.ElapsedMilliseconds) + "\n\r 本次同步数据失败，半小时内自动同步。。。。。。");
            }

            if (autoRun == "0")
            {
                Console.ReadKey();
            }

        }


        /// <summary>
        /// 采集源循环采集
        /// </summary>
        /// <param name="ds"></param>
        private static void getItemSourceMaster(DataSet ds)
        {
            //List<string> list = new List<string>();
            DataTable dt;

            //对于每个行数据采集列表
            for (int j = 0; j < ds.Tables[0].Rows.Count; j++)
            {
                #region 获取并初始化需要的通用行数据
                dt = null;

                int _sourceid = int.Parse(ds.Tables[0].Rows[j]["id"].ToString());//采集源ID
                int _opcid = int.Parse(ds.Tables[0].Rows[j]["opc_id"].ToString());//运营中心ID
                int _wdid = int.Parse(ds.Tables[0].Rows[j]["wd_id"].ToString());//运营中心ID
                int _class = int.Parse(ds.Tables[0].Rows[j]["class"].ToString());//类别：列表或关键字

                string _sourceurl = ds.Tables[0].Rows[j]["source_url"].ToString();//采集源首页
                string _gatherurl = ds.Tables[0].Rows[j]["gather_url"].ToString();//参数化、结构化的采集源
                string listp1 = ds.Tables[0].Rows[j]["list_p1"].ToString() == "-" ? "" : ds.Tables[0].Rows[j]["list_p1"].ToString();  //列表中变化参数1，同$pageno，
                string listp2 = ds.Tables[0].Rows[j]["list_p2"].ToString() == "-" ? "" : ds.Tables[0].Rows[j]["list_p2"].ToString();  //列表中变化参数2，同$pageno，
                string param_residual = ds.Tables[0].Rows[j]["param_residual"].ToString() == "-" ? "" : ds.Tables[0].Rows[j]["param_residual"].ToString(); ;//列表固定后缀参数

                //以下两列是关键字采集源列表
                string _keyword = ds.Tables[0].Columns.Contains("keyword") ? ds.Tables[0].Rows[j]["keyword"].ToString() : "";
                int _seqno = ds.Tables[0].Columns.Contains("seq_no") ? int.Parse(ds.Tables[0].Rows[j]["seq_no"].ToString()) : 0;

                string _url = _gatherurl.Replace("$param1", getTranslationStringDate(listp1)).Replace("$param2", getTranslationStringDate(listp2)).Replace("$kw", _keyword);//动态参数格式


                _url += param_residual;//包含页面变量用来循环地址
                string _infourl = ds.Tables[0].Rows[j]["info_url"].ToString();//信息表参数化,获取网址的时候替换   

                string _urlpattern = ds.Tables[0].Rows[j]["info_url"].ToString();//列表网址拼接格式
                string _listbegin = ds.Tables[0].Rows[j]["list_begin"].ToString(); //列表开始字符串
                string _listend = ds.Tables[0].Rows[j]["list_end"].ToString(); //列表结束字符串

                int _firstpageratio = int.Parse(ds.Tables[0].Rows[j]["first_page_ratio"].ToString());
                int _firstpageplus = int.Parse(ds.Tables[0].Rows[j]["first_page_plus"].ToString());
                int _firstpage = int.Parse(ds.Tables[0].Rows[j]["first_page"].ToString());//采集的第一页，大于1则表示首页信息和翻页信息不能通用格式
                int _gatherpages = int.Parse(ds.Tables[0].Rows[j]["gather_pages"].ToString());//非所有页时,实际采集页数

                _firstpage = _firstpage * _firstpageratio + _firstpageplus; //循环的起始页
                _gatherpages = _gatherpages * _firstpageratio + _firstpageplus;//循环的采集总页数


                int _totalpages = int.Parse(ds.Tables[0].Rows[j]["total_pages"].ToString());//列表总页数

                bool _isgatherallpages = bool.Parse(ds.Tables[0].Rows[j]["is_gather_all_pages"].ToString());//是否采集所有页面


                bool _isgenericgatherurl = bool.Parse(ds.Tables[0].Rows[j]["is_generic_gather_url"].ToString());//是否采集所有页面


                string _listpattern = ds.Tables[0].Rows[j]["list_pattern"].ToString().Replace("单引号", @"'").Replace("双引号", @"""").Replace("斜杠", @"\");//列表页面正则
                string _listcharset = ds.Tables[0].Rows[j]["list_charset"].ToString();//列表页面字符集

                bool _listispost = bool.Parse(ds.Tables[0].Rows[j]["list_ispost"].ToString());//是否采集所有页面


                bool _isgatherinfopages = bool.Parse(ds.Tables[0].Rows[j]["is_gather_infopages"].ToString()); //是否采集具体页面内容





                #endregion

                if (_isgatherinfopages)
                {

                }
                else //仅采集页面信息
                {
                    dt = createDatatable(_listpattern);

                    //不是通用的采集网址，先首页采集
                    if (!_isgenericgatherurl)
                    {
                        Console.WriteLine("source_id: " + _sourceid.ToString() + "\t\t pages:-1"  + "\t\t 关键字:" + _seqno.ToString() + " - " + _keyword);
                        getListOnly(dt, _class, _opcid, _wdid, _sourceid,_seqno, _keyword, _sourceurl, _listispost, _listcharset, _listpattern, _firstpage - 1, _infourl, _urlpattern, _listbegin, _listend);
                    }


                    if (_isgatherallpages)
                    {
                        for (int m = _firstpage; m <= _totalpages; m++)
                        {
                            Console.WriteLine("source_id: " + _sourceid.ToString() + "\t\t pages:" + m.ToString() + "\t\t 关键字:" + _seqno.ToString() + " - " + _keyword);
                            getListOnly(dt, _class, _opcid, _wdid, _sourceid, _seqno, _keyword, _url.Replace("$pageno", _firstpage.ToString()), _listispost, _listcharset, _listpattern, m, _infourl, _urlpattern, _listbegin, _listend);
                        }

                    }
                    else  //如果不是采集所有页，则把【$pageno】-->【gather_pages】页数，循环
                    {

                        for (int m = _firstpage; m < _firstpage + _gatherpages; m += _firstpageratio)
                        {
                            Console.WriteLine("source_id: " + _sourceid.ToString() + "\t\t pages:" + m.ToString() + "\t\t 关键字:" + _seqno.ToString()+" - "+ _keyword);
                            getListOnly(dt, _class, _opcid, _wdid, _sourceid, _seqno, _keyword, _url.Replace("$pageno", m.ToString()), _listispost, _listcharset, _listpattern, m, _infourl, _urlpattern, _listbegin, _listend);
                        }

                    }

                    //插入到通用模板表  t_gather_info_model
                    DAL.loadDataTableToDBModelTable(dt, "t_gather_list_model");
                }


            }
            ////全部采集完后，调用XGMOA上的同步存储过程--中午作业执行
            //DAL.execProcedureNonParamerters("pr_sync_gather_info_common");

        }

        /// <summary>
        /// 创建存储采集数据的容器DataTable
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static DataTable createDatatable(string pattern)
        {
            DataTable dt = new DataTable();
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


                dt.Columns.Add("class");
                dt.Columns.Add("opc_id");
                dt.Columns.Add("wd_id");
                dt.Columns.Add("source_id");
                dt.Columns.Add("url_pattern");
                dt.Columns.Add("keyword");
                dt.Columns.Add("seq_no");

            }

            return dt;

        }

        //备用没用到
        private static string PostData1(string url, string postData)
        {
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] data = encoding.GetBytes(postData);
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);

            myRequest.Method = "POST";
            myRequest.ContentType = "application/x-www-form-urlencoded";
            myRequest.ContentLength = data.Length;
            Stream newStream = myRequest.GetRequestStream();

            newStream.Write(data, 0, data.Length);
            newStream.Close();
            HttpWebResponse myResponse = null;

            try
            {
                myResponse = (HttpWebResponse)myRequest.GetResponse();
                StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.Default);
                string content = reader.ReadToEnd();
                reader.Close();
                return content;
            }
            catch (WebException ex)
            {
                myResponse = ex.Response as HttpWebResponse;
                using (Stream errData = myResponse.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(errData))
                    {
                        string res = sr.ReadToEnd();
                        return res;
                    }
                }

            }
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
        public bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
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
        /// <param name="charset"></param>
        /// <param name="pattern"></param>
        /// <param name="pageno"></param>
        /// <param name="infourl"></param>
        /// <param name="urlpattern"></param>
        /// <param name="listbegin"></param>
        /// <param name="listend"></param>
        /// <returns></returns>
        private static DataTable getListOnly(DataTable dt, int classid, int opcid, int wdid, int sourceid,int seq, string keyword, string url, bool ispost, string charset, string pattern, int pageno, string infourl, string urlpattern, string listbegin, string listend)
        {
                            
            #region 1.采集过程

            //采集开始时间
            string gatherBT = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            bool isRegMatch = false;//是否匹配到数据
            bool isException;//HTTP获取数据是否异常
            string response = string.Empty;//获取响应内容或者异常信息
            int gatherRows = 0;//正则匹配到的条数基本等于每个列表页面的链接数
            if (ispost)
            {
                response = PostData(url, charset, out isException);
            }
            else
            {
                //下列采集列表只能使用精简版的GET（具体原因没有去研究）
                if (sourceid == 52 || sourceid == 53 || sourceid == 33)
                {
                    response = test.Get(url, charset, out isException);
                }
                else
                {
                    response = GetData(url, charset, out isException);
                }
            }


            //没有异常
            if (!isException)
            {
                response = DealwithSpecialCharactersTag(response);
                Regex rg = new Regex(pattern, RegexOptions.IgnoreCase);//指定不区分大小写的匹配
                if (response != null || response.Length > 0)
                {
                    if (listbegin.Length > 0 && listend.Length > 0)
                    {
                        response = substringContentList(response, listbegin, listend);
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
                            dr["url"] = DealWithUrl(urlpattern, dr["url"].ToString(), out isDealed);

                            //已经处理成完整的URL，写入数据库的信息网址构成样式，直接用$url代替;
                            dr["url_pattern"] = isDealed ? "$url" : urlpattern;


                            //固定必须有的数据（采集源）
                            dr["source_id"] = sourceid.ToString();
                            dr["class"] = classid.ToString();
                            dr["opc_id"] = opcid.ToString();
                            dr["wd_id"] = wdid.ToString();
                            dr["keyword"] = keyword;
                            dr["seq_no"] = seq.ToString();
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
            pattern = isRegMatch ? "" : pattern;
            #endregion

            #region 3.将采集日志写入到数据库表
            int cnt = 1;
            int ok = DAL.WriteLog(classid, sourceid,seq, keyword, pageno, isRegMatch, isException, response, pattern, gatherBT, gatherET, gatherRows);
            while (ok != 1 && cnt < 3)
            {
                Console.WriteLine($" 第{cnt}次 重试写入采集日志表...");
                ok = DAL.WriteLog(classid, sourceid,seq, keyword ,pageno, isRegMatch, isException, response, pattern, gatherBT, gatherET, gatherRows);
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
        private static string getTranslationStringDate(string param)
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
        public string SendDataByGET(string Url, string postDataStr, ref CookieContainer cookie)
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
        public static string substringContentList(string content, string listbegin, string listend)
        {
            Regex rg = new Regex(listbegin + ".*?" + listend);
            return rg.Match(content).ToString();
        }

        /// <summary>
        /// 原始内容处理（去空值和脚本内容）
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string DealwithSpecialCharactersTag(string content)
        {
            string result = null;
            content = content.Replace("\t", "").Replace("\r", "").Replace("\n", "").Replace("&nbsp;", "");

            //将内容中的脚本去除
            content = Regex.Replace(Regex.Replace(Regex.Replace(content, @"<script.*?</.*?script.*?>", ""), @"replace.*?\(.*?\)", ""), "var.*?/.*?/", "");
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
        public static string DealWithInput(string input, out bool ifLegal)
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
        public static string DealWithBlank(string input)
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
        /// 处理采集的发布日期格式
        /// </summary>
        /// <param name="publishDate"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string DealWithPublishDate(string publishDate, string url)
        {
            //清空发布日期中的标签
            string _publishDate = Regex.Replace(publishDate, "<.*?>", "");
            DateTime _result;
            if (_publishDate.Length == 0) //如果为空，则设置为明天
            {
                return DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            }
            else if (DateTime.TryParse(_publishDate, out _result)) //可以转换为时间
            {
                return _result.ToString("yyyy-MM-dd");
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
        /// 处理采集的标题格式
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string DealWithTitle(string title)
        {
            string result = Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(title.ToLower(), "<font.*?>", ""), "</font.*?>", ""), " ", ""), "<span.*?>", ""), "</span.*?>", "");
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
        /// 处理采集的Url格式
        /// </summary>
        /// <param name="urlpattern">信息组成样式</param>
        /// <param name="url">返回处理后的具体信息URL</param>
        /// <param name="isDealed">是否已经处理成完整的信息URL（http;//xxx.com）,是的话，就将链接样式设置为$URL</param>
        /// <returns></returns>
        public static string DealWithUrl(string urlpattern, string url, out bool isDealed)
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
        public static string DealWithUrlPattern(bool isDealed, string urlPattern)
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
        public static string OnGetItemSourceList(string autoRun)
        {

            string queryString = @"Select class,wd_id,opc_id,id,source_url,gather_url,list_p1,list_p2,param_residual
                                                 ,first_page,gather_pages,total_pages,is_gather_all_pages
                                                 ,list_pattern,list_charset,list_ispost,info_url
                                                 ,is_gather_infopages,is_generic_gather_url
                                                 ,list_begin,list_end,first_page_ratio,first_page_plus
                                                from t_item_source_list where valid=1";

            return ValidateInput(autoRun, queryString);

        }

        /// <summary>
        /// 获取采集关键字类型查询语句 
        /// </summary>
        /// <param name="autoRun"></param>
        /// <returns></returns>
        public static string OnGetItemSourceKeyword(string autoRun)
        {
            string queryString = @"select class,wd_id,opc_id,id,source_url,gather_url,list_p1,list_p2,param_residual
                                                 ,first_page,gather_pages,total_pages,is_gather_all_pages
                                                 ,list_pattern,list_charset,list_ispost,info_url
                                                 ,is_gather_infopages,is_generic_gather_url
                                                 ,list_begin,list_end,first_page_ratio,first_page_plus
                                                 ,value as keyword,seq as seq_no
                                    from t_item_source_keyword  cross apply dbo.fn_clr_split_by_separator(keywords,'|') as a
                                    where valid=1";
            return ValidateInput(autoRun, queryString);
        }

        /// <summary>
        /// 验证用户输入结果，获取带过滤条件的字符串格式的查询
        /// </summary>
        /// <param name="autoRun"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public static string ValidateInput(string autoRun, string queryString)
        {
            bool legal = false;
            string input = "", input2 = "", result = "";

            if (autoRun == "0")
            {
                do
                {
                    Console.WriteLine("\n\r===请输入正确的采集的数据源【ID】，多个用空格分隔,数字【0】代表全部,【ENTER键】确认=======\n\r");
                    //DateTime t1 = DateTime.Now.ToUniversalTime();
                    input = Console.ReadLine();

                    result = DealWithInput(input, out legal);
                }
                while (!legal);

                int arrlen = input.Split(new char[] { ' ' }).Length;



                if (arrlen == 1)//单个值
                {
                    if (input != "0")//不是全部的情况下，选择是连续采集还是单个采集
                    {
                        do
                        {
                            Console.WriteLine("\n\r===请选择输入 <1：仅采集输入的采集源ID > < 2：从输入的采集源ID开始采集 >,【ENTER键】确认=======\n\r");
                            input2 = Console.ReadLine();
                        }
                        while (input2 != "1" && input2 != "2");

                        if (input2 == "1")
                        {
                            queryString += " and id =" + result;
                        }
                        else
                        {
                            queryString += " and id >=" + input;
                        }
                    }

                }
                else//多个值
                {
                    queryString += " and id in(" + result + ")";
                }
            }

            return queryString;
        }

    }
}
