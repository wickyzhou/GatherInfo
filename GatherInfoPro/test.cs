using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace 控制台程序获取数据
{   //dd233f88ggg
    static class test
    {
        static String contentType = "application/x-www-form-urlencoded";
        static String accept = "image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-shockwave-flash, application/x-silverlight, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, application/x-ms-application, application/x-ms-xbap, application/vnd.ms-xpsdocument, application/xaml+xml, application/x-silverlight-2-b1, */*";
        static String userAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.116 Safari/537.36";

        public static String Get(String url, String encode, out bool isException)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                InitHttpWebRequestHeaders(request);
                request.Method = "GET";
                var html = ReadHtml(request, encode);
                isException = false;
                return html;
            }
            catch (Exception e)
            {
                isException = true;
                return e.Message.ToString();
            }

        }

        public static String Post(String url, String param, String encode)
        {
            Encoding encoding = System.Text.Encoding.UTF8;
            byte[] data = encoding.GetBytes(param);
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            InitHttpWebRequestHeaders(request);
            request.Method = "POST";
            request.ContentLength = data.Length;
            var outstream = request.GetRequestStream();
            outstream.Write(data, 0, data.Length);
            var html = ReadHtml(request, encode);
            return html;
        }

        private static void InitHttpWebRequestHeaders(HttpWebRequest request)
        {
            request.ContentType = contentType;
            request.Accept = accept;
            request.UserAgent = userAgent;
        }

        private static String ReadHtml(HttpWebRequest request, String encode)
        {
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(encode));
            String content = reader.ReadToEnd();
            reader.Close();
            stream.Close();
            return content;
        }

    }
}
