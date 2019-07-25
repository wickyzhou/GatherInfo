using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace 控制台程序获取数据
{
    public static class Util
    {
        public static void SetCertificatePolicy()
        {
            ServicePointManager.ServerCertificateValidationCallback
                       += RemoteCertificateValidate;
        }

        /// <summary>
        /// Remotes the certificate validate.
        /// </summary>
        private static bool RemoteCertificateValidate(
           object sender, X509Certificate cert,
            X509Chain chain, SslPolicyErrors error)
        {
            // trust any certificate!!!
            //System.Console.WriteLine("Warning, trust any certificate");
            return true;
        }

        /// <summary>
        /// 将填的info_url中的关键字中文转码为带百分号的
        /// </summary>
        /// <param name="urlpattern"></param>
        /// <param name="url"></param>
        /// <param name="isDealed"></param>
        /// <returns></returns>
        //private static string DealWithUrlPattern(bool isDealed, string urlPattern)
        //{
        //    //信息页面，编码和不编码，用GETDATA访问的时候，没有任何区别。
        //    string a = HttpUtility.UrlDecode("%e6%8b%9b%e6%a0%87");
        //    string b = HttpUtility.UrlEncode("招标");
        //    return "s";

        //}



    }
}
