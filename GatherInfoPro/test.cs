using System;


namespace 控制台程序获取数据
{   //dd233f88ggg
    static class Test
    {
        //static readonly String contentType = "application/x-www-form-urlencoded";
        //static readonly String accept = "image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-shockwave-flash, application/x-silverlight, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, application/x-ms-application, application/x-ms-xbap, application/vnd.ms-xpsdocument, application/xaml+xml, application/x-silverlight-2-b1, */*";
        //static readonly String userAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.116 Safari/537.36";

        //public static String Get(String url, String encode, out bool isException)
        //{
        //    try
        //    {

        //        HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
        //       InitHttpWebRequestHeaders(request);
        //        request.Method = "GET";
        //        //request.Headers.Add("UserAgent","PostmanRuntime/7.13.0");
        //        //request.Headers.Add("Accept", "*/*");
        //        //request.Headers.Add("CacheControl", "no-cache");
        //        //request.Headers.Add("Connection", "keep-alive");
        //        //request.Headers.Add("AcceptEncoding", "gzip, deflate");
        //        //request.Headers.Add("Host","jsb.nea.gov.cn");
        //        var html = ReadHtml(request, encode);
        //        isException = false;
        //        return html;
        //    }
        //    catch (Exception e)
        //    {
        //        isException = true;
        //        return e.Message.ToString();
        //    }

        //}

        //备用没用到
        //public static String Post(String url, String param, String encode)
        //{
        //    Encoding encoding = System.Text.Encoding.UTF8;
        //    byte[] data = encoding.GetBytes(param);
        //    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
        //    InitHttpWebRequestHeaders(request);
        //    request.Method = "POST";
        //    request.ContentLength = data.Length;
        //    var outstream = request.GetRequestStream();
        //    outstream.Write(data, 0, data.Length);
        //    var html = ReadHtml(request, encode);
        //    return html;
        //}


        //public static void InitHttpWebRequestHeaders(HttpWebRequest request)
        //{
        //    request.ContentType = contentType;
        //    request.Accept = accept;
        //    request.UserAgent = userAgent;

        //}


        //public static String ReadHtml(HttpWebRequest request, String encode)
        //{
        //    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
        //    Stream stream = response.GetResponseStream();
        //    StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(encode));
        //    String content = reader.ReadToEnd();
        //    reader.Close();
        //    stream.Close();
        //    return content;

        //}

        //备用没用到
        //public static string PostData1(string url, string postData)
        //{
        //    ASCIIEncoding encoding = new ASCIIEncoding();
        //    byte[] data = encoding.GetBytes(postData);
        //    HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);

        //    myRequest.Method = "POST";
        //    myRequest.ContentType = "application/x-www-form-urlencoded";
        //    myRequest.ContentLength = data.Length;
        //    Stream newStream = myRequest.GetRequestStream();

        //    newStream.Write(data, 0, data.Length);
        //    newStream.Close();
        //    HttpWebResponse myResponse;

        //    try
        //    {
        //        myResponse = (HttpWebResponse)myRequest.GetResponse();
        //        StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.Default);
        //        string content = reader.ReadToEnd();
        //        reader.Close();
        //        return content;
        //    }
        //    catch (WebException ex)
        //    {
        //        myResponse = ex.Response as HttpWebResponse;
        //        using (Stream errData = myResponse.GetResponseStream())
        //        {
        //            using (StreamReader sr = new StreamReader(errData))
        //            {
        //                string res = sr.ReadToEnd();
        //                return res;
        //            }
        //        }

        //    }
        //}


        /// <summary>
        /// 将CookieContainer转换成为可以添加到RequestHeader.Cookie字符串
        /// </summary>
        /// <param name="cc"></param>
        /// <returns></returns>
        //private static string CookieContainerToString(CookieContainer cc)
        //{
        //    StringBuilder sbc = new StringBuilder();
        //    List<Cookie> cooklist = GetAllCookies(cc);
        //    foreach (Cookie cookie in cooklist)
        //    {
        //        //cookie后面不能加分号
        //        sbc.AppendFormat($";{cookie.Name}={cookie.Value}");
        //    }
        //    return sbc.ToString().Substring(1);
        //}

        /// <summary>
        /// 将CookieContainer里面所有的Cookie加入到List中
        /// </summary>
        /// <param name="cc"></param>
        /// <returns></returns>
        //private static List<Cookie> GetAllCookies(CookieContainer cc)
        //{
        //    List<Cookie> lstCookies = new List<Cookie>();

        //    Hashtable table = (Hashtable)cc.GetType().InvokeMember("m_domainTable",
        //    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField |
        //    System.Reflection.BindingFlags.Instance, null, cc, new object[] { });

        //    foreach (object pathList in table.Values)
        //    {
        //        SortedList lstCookieCol = (SortedList)pathList.GetType().InvokeMember("m_list",
        //        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField
        //        | System.Reflection.BindingFlags.Instance, null, pathList, new object[] { });
        //        foreach (CookieCollection colCookies in lstCookieCol.Values)
        //            foreach (Cookie c in colCookies) lstCookies.Add(c);
        //    }

        //    return lstCookies;
        //}

        /// <summary>
        /// 百度的一个认证，看不懂
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        //private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        //{   // 总是接受  
        //    return true;
        //}



        /// <summary>
        /// 创建存储采集数据具体信息表的容器DataTable
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        //private static List<string> GetInfoPageAllLoadFields(DataTable dtInfoPageWaiting)
        //{
        //    List<string> fields = new List<string>();
        //    List<string> FieldsInSingleColumn = new List<string>();
        //    string f1; string f2; //string f3;

        //    for (int i = 0; i < dtInfoPageWaiting.Rows.Count; i++)
        //    {
        //        f1 = dtInfoPageWaiting.Rows[i]["info_fixed_fields"].ToString();
        //        f2 = dtInfoPageWaiting.Rows[i]["info_var_fields"].ToString();
        //        fields.Add(f1);
        //        fields.Add(f2);
        //    }

        //    foreach (var items in fields.Distinct().ToList())
        //    {
        //        var item = items.Split(',');
        //        foreach (var item1 in item)
        //        {
        //            if (!FieldsInSingleColumn.Contains(item1))
        //                FieldsInSingleColumn.Add(item1);
        //        }
        //    }
        //    return FieldsInSingleColumn;
        //}



    }
}
