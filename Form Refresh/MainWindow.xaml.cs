using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Form_Refresh
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static List<ScheduleTimer> _schedularTimerList = new List<ScheduleTimer>();


        public MainWindow()
        {
            InitializeComponent();
            DgResult.ItemsSource = _refreshDetailsList;
            _UpdateStatusDelegate = UpdateStatus;
            BtnStopRefresh.IsEnabled = false;
        }

        CookieContainer _cookieCon;

        List<RefreshDetails> _refreshDetailsList = new List<RefreshDetails>();

        delegate void UpdateStatusDelegate(string status, int value);

        UpdateStatusDelegate _UpdateStatusDelegate;

        BackgroundWorker _bgw;

        bool _IsWorketStopped;

        void UpdateStatus(string message, int value)
        {
            _refreshDetailsList[value].Status = message;
            Probar.Value = value+1;
            DgResult.Items.Refresh();
        }

        HtmlResult GetHtml(string url)
        {
            var result = new HtmlResult() ;
            try
            {
                using (var client = new WebClient())
                {
                    result.Html=client.DownloadString(url);
                }
                result.Error = false;
            }
            catch(Exception ex)
            {
                result.Error = true;
                result.Html = ex.Message;
            }
            return result;
        }

        string[] GetFormDetails(string html)
        {
            //var html = GetHtml("http://www.yeeyi.com/bbs/index.php");
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var retStr = new string[2];
            var formNode = doc.DocumentNode.SelectSingleNode("//form[@id='loginform_LXm3m']");
            if (formNode == null)
                retStr[0] = null;
            else
            {
                var loginUrl = "http://www.yeeyi.com/bbs/" + formNode.GetAttributeValue("action", "");
                retStr[0] = HtmlAgilityPack.HtmlEntity.DeEntitize(loginUrl);
            }
            var formHash = doc.DocumentNode.SelectSingleNode("//input[@name='formhash']");
            if (formHash == null)
                retStr[1] = null;
            else
            {
                var formHashValue = formHash.GetAttributeValue("value", "");
                retStr[1] = formHashValue;
            }
            return retStr;
        }

        void EnableUI(bool res)
        {
            BtnLoadUser.IsEnabled = res;
            BtnStartRefresh.IsEnabled = res;
            BtnStopRefresh.IsEnabled = !res;
        }

        HtmlResult PostRequest(string url, string querry)
        {
            var result = new HtmlResult();
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.ServicePoint.Expect100Continue = false;
                req.KeepAlive=true;
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.CookieContainer = _cookieCon;
                req.Headers.Add("Accept-Encoding: gzip,deflate,sdch");
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";
                req.ContentType = "application/x-www-form-urlencoded";
                req.Referer = "http://www.yeeyi.com/bbs/index.php";
                req.Method = "POST";
                req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                var bytes = Encoding.UTF8.GetBytes(querry);
                using (var writer = req.GetRequestStream())
                {
                    writer.Write(bytes, 0, bytes.Length);
                }
                var response = req.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    result.Html = reader.ReadToEnd();
                }
                result.Error = false;
            }
            catch (Exception ex)
            {
                result.Html = ex.Message;
                result.Error = true;
            }
            return result;
        }

        HtmlResult GetHtmlWithCookie(string url)
        {
            var result = new HtmlResult();
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.ServicePoint.Expect100Continue = false;
                req.KeepAlive = true;
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.CookieContainer = _cookieCon;
                req.Headers.Add("Accept-Encoding: gzip,deflate,sdch");
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";
                req.ContentType = "application/x-www-form-urlencoded";
                req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                //req.Referer = "http://www.yeeyi.com/bbs/index.php";
                req.Method = "GET";

                var response = req.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    result.Html = reader.ReadToEnd();
                }

                result.Error = false;
            }
            catch (Exception ex)
            {
                result.Html = ex.Message;
                result.Error = true;
            }
            return result;
        }

        void StartRefresh()
        {
            EnableUI(false);
            Probar.Minimum = 0;
            Probar.Maximum = _refreshDetailsList.Count;
            Probar.Value = 0;
            _bgw = new BackgroundWorker();
            _bgw.DoWork += _bgw_DoWork;
            _bgw.RunWorkerCompleted += _bgw_RunWorkerCompleted;
            _bgw.RunWorkerAsync();
        }

        void _bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            EnableUI(true);
            _bgw.Dispose();
        }

        void _bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            int i = -1;
            foreach (var rd in _refreshDetailsList)
            {
                if (_IsWorketStopped)
                    break;
                i++;
                try
                {
                    _cookieCon = new CookieContainer();
                    var htm = GetHtmlWithCookie("http://www.yeeyi.com/bbs/index.php");
                    if (htm.Error)
                    {
                        Dispatcher.Invoke(_UpdateStatusDelegate, htm.Html, i);
                        continue;
                    }

                    //File.WriteAllText(@"c:\temp\rMainPage.htm", htm.Html);
                    
                    var formDetails = GetFormDetails(htm.Html);
                    if(formDetails[0]==null||formDetails[1]==null)
                    {
                        Dispatcher.Invoke(_UpdateStatusDelegate, "Error: cant get formhash value.", i);
                        continue;
                    }

                    var querry = "formhash="+formDetails[1]+"&referer=&username="+rd.UserName+"&password="+rd.Password+"&loginsubmit=Login";
                    
                    System.Threading.Thread.Sleep(2000);
                    
                    htm = PostRequest(formDetails[0], querry);
                    if(htm.Error)
                    {
                        Dispatcher.Invoke(_UpdateStatusDelegate, htm.Html, i);
                        continue;
                    }

                    //File.WriteAllText(@"c:\temp\rLoggedIn.htm", htm.Html);

                    System.Threading.Thread.Sleep(2000);

                    htm = GetHtmlWithCookie(rd.Url);

                    if (htm.Error)
                    {
                        Dispatcher.Invoke(_UpdateStatusDelegate, htm.Html, i);
                        continue;
                    }

                    //File.WriteAllText(@"c:\temp\rRefresh.htm", htm.Html);

                    System.Threading.Thread.Sleep(2000);

                    htm = GetHtmlWithCookie("http://www.yeeyi.com/bbs/member.php?mod=logging&action=logout&formhash=" + formDetails[1]);

                    if (htm.Error)
                    {
                        Dispatcher.Invoke(_UpdateStatusDelegate, htm.Html, i);
                        continue;
                    }

                    System.Threading.Thread.Sleep(2000);

                    Dispatcher.Invoke(_UpdateStatusDelegate, "Done", i);

                    _cookieCon = null;
                    //File.WriteAllText(@"c:\temp\rLogout.htm", htm.Html);

                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(_UpdateStatusDelegate, ex.Message, i);
                }
            }
        }

        void LoadUsers()
        {
            var tlist = new List<RefreshDetails>(_refreshDetailsList);           
            try
            {
                var temp = File.ReadAllText("users.txt");
                var lines = temp.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var param = line.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    _refreshDetailsList.Add(new RefreshDetails { UserName = param[0], Password = param[1], Url = param[2], Status = "" });
                }

            }
            catch (Exception ex)
            {
                _refreshDetailsList = null;
                _refreshDetailsList = new List<RefreshDetails>(tlist);
                MessageBox.Show(ex.Message);
                MessageBox.Show("File corrupted");
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
            DgResult.Items.Refresh();
        }

        private void BtnStartRefresh_Click(object sender, RoutedEventArgs e)
        {
            _IsWorketStopped = false;
            if (_refreshDetailsList.Count == 0)
            {
                MessageBox.Show("Please add details of the webpage");
                return;
            }
            StartRefresh();
        }

        private void BtnStopRefresh_Click(object sender, RoutedEventArgs e)
        {
            _IsWorketStopped = true;
            BtnStopRefresh.IsEnabled = false;
        }
    }
}
