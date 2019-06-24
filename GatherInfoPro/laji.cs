
namespace 控制台程序获取数据
{
    class laji
    {/*
        private  string CookieContainerToString(CookieContainer cc)
        {
            StringBuilder sbc = new StringBuilder();
            List<Cookie> cooklist = GetAllCookies(cc);
            foreach (Cookie cookie in cooklist)
            {
                //cookie后面不能加分号
                sbc.AppendFormat($"{cookie.Name}={cookie.Value}\r\n");
            }
            return sbc.ToString();
        }

        private  List<Cookie> GetAllCookies(CookieContainer cc)
        {
            List<Cookie> lstCookies = new List<Cookie>();

            Hashtable table = (Hashtable)cc.GetType().InvokeMember("m_domainTable",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField |
            System.Reflection.BindingFlags.Instance, null, cc, new object[] { });

            foreach (object pathList in table.Values)
            {
                SortedList lstCookieCol = (SortedList)pathList.GetType().InvokeMember("m_list",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField
                | System.Reflection.BindingFlags.Instance, null, pathList, new object[] { });
                foreach (CookieCollection colCookies in lstCookieCol.Values)
                    foreach (Cookie c in colCookies) lstCookies.Add(c);
            }

            return lstCookies;
        }


        private static string ConvertToMD5String(String argString)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.Default.GetBytes(argString);
            byte[] result = md5.ComputeHash(data);
            String strReturn = String.Empty;
            for (int i = 0; i < result.Length; i++)
                strReturn += result[i].ToString("x").PadLeft(2, '0');
            return strReturn;
        }
    }
    */


        /*
        
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

         */


        /*
             /// <summary>
        /// POST方式获取网址响应
        /// </summary>
        /// <param name="gatherUrl"></param>
        /// <param name="charset"></param>
        /// <param name="isException"></param>
        /// <returns></returns>
        private static string PostData(string gatherUrl, string charset, string requestHeaders, out bool isException, string newCookie = "")
        {
            try
            {
                //CookieContainer cc = new CookieContainer();
                //数据库网址URL以？号分割, POSTformdata里面也有|，因此不能用split,用最多拆分成2部分的重载
                string postData = gatherUrl.Split(new char[] { '|' }, 2)[1];
                string url = gatherUrl.Split(new char[] { '|' }, 2)[0];

                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] data = encoding.GetBytes(postData);
                System.GC.Collect();
                //byte[] data = Encoding.Unicode.GetBytes(postData);
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                myRequest.Timeout = 20000;
                myRequest.Method = "POST";
                //强制取消缓存，同浏览器Disable Cache
                HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                myRequest.CachePolicy = noCachePolicy;
                myRequest.ServicePoint.Expect100Continue = false;//卡主不动无法提交
                myRequest.AllowAutoRedirect = false;
                myRequest.KeepAlive = false;//基础连接已经关闭: 服务器关闭了本应保持活动状态的连接。
                AddRequestHeaders(myRequest, requestHeaders); //浏览器显示的requestHeaders里面有些是request属性，和headers平级的，只能用.ContentType来设置值。其余可以用headers.Add方法添加
                if (myRequest.Headers["User-Agent"] == null)
                {
                    myRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.116 Safari/537.36";
                }
                if (myRequest.Headers["Content-Type"] == null)
                {
                    myRequest.ContentType = "application/x-www-form-urlencoded";
                }
                if (newCookie.Length > 0)
                {
                    myRequest.Headers.Add("Cookie", newCookie);
                }
                //常见的两种方式添加到headers，一次只能添加一行
                //myRequest.Headers.Add("Cookie", "JSESSIONID=2X5BdGzLV5d1ZZygSvQWpQRHf8BJxsJprGp1chnMVffXCrWBgSnL!-1523611838");
                //myRequest.Headers.Add(@"Pragma: no-cache");
                //myRequest.ContentLength =-1; //基础连接失败，取消改值设置。
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
                    if (myResponse != null)
                    {
                        myResponse.Close();
                    }
                    myRequest.Abort();

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
        private static string GetData(string url, string charset, string requestHeaders, out bool isException, string newCookie = "")
        {
            try
            {
                Util.SetCertificatePolicy();//验证安全，未能为 SSL/TLS 安全通道建立信任关系
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                myRequest.Timeout = 20000;
                myRequest.Method = "GET";
                //强制取消缓存，同浏览器Disable Cache
                HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                myRequest.CachePolicy = noCachePolicy;
                myRequest.KeepAlive = false;//基础连接已经关闭: 服务器关闭了本应保持活动状态的连接。
                myRequest.ServicePoint.Expect100Continue = false;//卡主不动无法提交
                myRequest.AllowAutoRedirect = false;
                AddRequestHeaders(myRequest, requestHeaders);
                if (myRequest.Headers["User-Agent"] == null)
                {
                    myRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.116 Safari/537.36";
                }
                if (newCookie.Length > 0)
                {
                    myRequest.Headers.Add("Cookie", newCookie);
                }
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

         */
    }
}
