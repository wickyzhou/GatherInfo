using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace 控制台程序获取数据
{
    public partial class Form1 : Form
    {
        [DllImport("User32.DLL")]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("User32.DLL")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent,
        IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        public int IDM_VIEWSOURCE = 2139;
        public uint WM_COMMAND = 0x0111;

        public int pageCount = 1;
        private string _navigateUrl;


        public Form1(string url)
        {
            InitializeComponent();
            this._navigateUrl = url;

            Button1_Click(null, null);
            Button2_Click(null, null);
            Thread thread = new Thread(new ThreadStart(Button2Click));
            thread.Start();//启动新线程

        }

        private void Button2Click()
        {
            Button2_Click(null, null);
        }

        private void GetHtmlDocument()
        {
            //string qq = webBrowser1.Document.GetElementById("div1").OuterHtml;
            richTextBox1.Text += "已完成,获取到响应文本，可返回";
            richTextBox2.Text = "\n\n===========================\n\n" + richTextBox2.Text + webBrowser1.DocumentText;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            //webBrowser1.Navigate("file:///C:/Users/ZZ/Desktop/HelloWorld.html");
            webBrowser1.Navigate(_navigateUrl);
            while (webBrowser1.ReadyState < WebBrowserReadyState.Complete)
                Application.DoEvents();     //若没加载完则继续加载
            GetHtmlDocument();
            //string q=webBrowser1.Document.All(0); //0：里面是所有源代码
            //Thread.Sleep(5000);

            //Encoding encoding = Encoding.GetEncoding(webBrowser1.Document.Encoding);
            //StreamReader stream = new StreamReader(webBrowser1.DocumentStream, encoding);
            //richTextBox1.Text = stream.ReadToEnd();

            //richTextBox2.Text = webBrowser1.Document.Body.InnerHtml;
            //richTextBox3.Text = webBrowser1.Document.Body.OuterHtml;

            //HtmlDocument all = webBrowser1.Document;
            //先根据ID来查找，如果没有此元素则通过标签获取一组元素，遍历获取值
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            HtmlElement nextPage;
            nextPage = webBrowser1.Document.GetElementById("next");
            if (nextPage == null)
            {
                var aTags = webBrowser1.Document.GetElementsByTagName("a");
                for (int i = aTags.Count - 1; i > 0; i--)
                {
                    if (aTags[i].InnerText != null)
                    {
                        if (Regex.Replace(aTags[i].InnerText, @"\s", "") == "下一页" || Regex.Replace(aTags[i].InnerText, @"\s", "") == "下页")
                        {
                            nextPage = aTags[i];
                            break;
                        }
                    }

                }
            }
            //模拟点击的时候，会执行Navigating，然后连续两次文档加载完成？？？
            while (pageCount < 5)
            {


                nextPage.InvokeMember("click");
                Rectangle reg = new Rectangle(0, 0, webBrowser1.Document.Window.Size.Width, webBrowser1.Document.Window.Size.Height);
                webBrowser1.Invalidate(reg, true);
                webBrowser1.Update();

                webBrowser1.Refresh();
                while (webBrowser1.ReadyState < WebBrowserReadyState.Complete)
                    Application.DoEvents();     //若没加载完则继续加载
                GetHtmlDocument();
                pageCount++;
            }

            //执行界面动态JS
            //string result = (string)webBrowser1.Document.InvokeScript("_$wa", new object[] { "roGk" });
            //richTextBox2.Text = result;
            //richTextBox1.Text = webBrowser1.DocumentText;

            //执行界面动态JS
            //string result = (string)webBrowser1.Document.InvokeScript("write", new object[] { "s" });
            //richTextBox2.Text = result;
            //richTextBox1.Text = webBrowser1.DocumentText;

        }

        private void Button3_Click(object sender, EventArgs e)
        {
            //richTextBox1.Text += webBrowser1.Document.DomDocument.ToString();
            //richTextBox2.Text += webBrowser1.Document.Body.InnerHtml;
            //richTextBox3.Text += webBrowser1.Document.Body.OuterHtml;

            //IntPtr vHandle = webBrowser1.Handle;
            //vHandle = FindWindowEx(vHandle, IntPtr.Zero, "Shell Embedding", null);
            //vHandle = FindWindowEx(vHandle, IntPtr.Zero, "Shell DocObject View", null);
            //vHandle = FindWindowEx(vHandle, IntPtr.Zero, "Internet Explorer_Server", null);
            //SendMessage(vHandle, WM_COMMAND, IDM_VIEWSOURCE, (int)Handle);
            button2.PerformClick();
        }


        private void WebBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

            WebBrowser wb = (sender as WebBrowser);
            if (e.Url.ToString() != wb.Url.ToString())//多个frame会多次触发
                return;
            if (wb.ReadyState != WebBrowserReadyState.Complete) //不是完成状态会多次触发
                return;
            if (e.Url.AbsolutePath != wb.Url.AbsolutePath)
                return;
            //while (webBrowser1.ReadyState < WebBrowserReadyState.Complete)
            //    Application.DoEvents();
            // 加载完第一次后这个ReadyState==Complete 已经相等了


            //if (!wb.IsBusy)//已经加载完毕
            //{
            //    //GetHtmlDocument();
            //    richTextBox1.Text += "已完成,获取到响应文本，可返回";
            //    richTextBox2.Text = "\n\n===========================\n\n" + richTextBox2.Text + wb.DocumentText;
            //}

            webBrowser1.Refresh();

        }

        /*
            //Thread.Sleep(5000);
            //获取源码对象HTML
            //HtmlElement item = wb.Document.All[0];
            //string result = wb.Document.GetElementById("next").OuterHtml;

            //if (sourceUrl.Contains("chinaunicombidding"))
            //{
            //    string funName = "";
            //    string funValue = "";
            //    string q = (string)wb.Document.InvokeScript("write", new object[] { "s" });
            //    IntPtr vHandle = wb.Handle;
            //    vHandle = FindWindowEx(vHandle, IntPtr.Zero, "Shell Embedding", null);
            //    vHandle = FindWindowEx(vHandle, IntPtr.Zero, "Shell DocObject View", null);
            //    vHandle = FindWindowEx(vHandle, IntPtr.Zero, "Internet Explorer_Server", null);
            //    SendMessage(vHandle, WM_COMMAND, IDM_VIEWSOURCE, (int)wb.Handle);
            //    SendMessage(vHandle, WM_GETTEXT, IDM_VIEWSOURCE, (int)wb.Handle);
            //}
            //string qq=wb.Document.Body.InnerHtml;
            //获取响应文档上面的编码，然后用其对应的编码编译其Stream，读取Stream，解决编码问题（功能同GetResponseEncoding）
         */



        /* 模拟登陆步骤：
         * 1. 获取页面的登陆框，和密码框，后台赋值
         * 2. 委托点击。
        HtmlElement element1 = webBrowser1.Document.GetElementById("LoginName");
            element1.InnerText = uid;
          
        HtmlElement element2 = webBrowser1.Document.GetElementById("password");
                element2.InnerText = pwd;

        webBrowser1.Document.GetElementById("log_in").InvokeMember("Click");
        */


    }
}
