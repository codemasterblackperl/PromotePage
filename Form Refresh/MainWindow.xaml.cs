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
using System.Windows.Threading;

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
            _UpdateStatusDelegate = new UpdateStatusDelegate(UpdateStatus);
            _updateLogDelegate = new UpdateLogDelegate(UpdateLog);
            BtnStopRefresh.IsEnabled = false;
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Tick += _refreshTimer_Tick;
            _refreshTimer.Interval = new TimeSpan(0, 10, 0);
            _refreshTimer.Stop();
            _refreshTimer.IsEnabled = false;
            _schedulerTimer = new DispatcherTimer();
            _schedulerTimer.Interval = new TimeSpan(0, 1, 0);
            _schedulerTimer.Tick += _schedulerTimer_Tick;
            _schedulerTimer.Start();
            _isTimerEnabled = false;
            UpdateLog("Software loaded");
            try
            {
                foreach (string readAllLine in File.ReadAllLines("sch.txt"))
                {
                    try
                    {
                        string[] strArray = readAllLine.Split(new string[1]
                        {
              ","
                        }, StringSplitOptions.RemoveEmptyEntries);
                        _schedularTimerList.Add(new ScheduleTimer()
                        {
                            From = int.Parse(strArray[0]),
                            To = int.Parse(strArray[1]),
                            Interval = int.Parse(strArray[2])
                        });
                    }
                    catch
                    {
                    }
                }
                UpdateLog("Settings loaded");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                UpdateLog(ex.Message);
                UpdateLog("Settings file not found.\nSet to default settings");
            }
        }

        private void _schedulerTimer_Tick(object sender, EventArgs e)
        {
            bool flag = false;
            _schedulerTimer.Stop();
            _schedulerTimer.IsEnabled = false;
            if ((_bgw == null || !_bgw.IsBusy) && _isTimerEnabled)
            {
                foreach (ScheduleTimer schedularTimer in _schedularTimerList)
                {
                    if (schedularTimer.From <= DateTime.Now.Hour && schedularTimer.To > DateTime.Now.Hour)
                    {
                        _refreshTimer.Interval = new TimeSpan(0, schedularTimer.Interval, 0);
                        flag = true;
                        break;
                    }
                }
            }
            if (flag)
            {
                _refreshTimer.IsEnabled = true;
                _refreshTimer.Start();
            }
            else
            {
                _schedulerTimer.IsEnabled = true;
                _schedulerTimer.Start();
            }
        }

        private void _refreshTimer_Tick(object sender, EventArgs e)
        {
            _refreshTimer.Stop();
            _refreshTimer.IsEnabled = false;
            if (_bgw != null && _bgw.IsBusy)
                return;
            StartRefresh();
        }

        CookieContainer _cookieCon;

        List<RefreshDetails> _refreshDetailsList = new List<RefreshDetails>();

        private int _rnd = 1;
        private bool _isTimerEnabled;

        delegate void UpdateStatusDelegate(string status, int value);
        delegate void UpdateLogDelegate(string message);

        UpdateStatusDelegate _UpdateStatusDelegate;

        UpdateLogDelegate _updateLogDelegate;

        private DispatcherTimer _refreshTimer;
        private DispatcherTimer _schedulerTimer;

        BackgroundWorker _bgw;

        bool _IsWorketStopped;

        void UpdateStatus(string message, int value)
        {
            _refreshDetailsList[value].Status = message;
            Probar.Value = value+1;
            DgResult.Items.Refresh();
        }

        private void UpdateLog(string message)
        {
            TextBox txtLog = TxtLog;
            string str = txtLog.Text + (object)DateTime.Now + ": " + message + ".\n";
            txtLog.Text = str;
        }

        private void BtnLoadUser_Click(object sender, RoutedEventArgs e)
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
            if (_isTimerEnabled)
            {
                _refreshTimer.Stop();
                _refreshTimer.IsEnabled = false;
            }
            StartRefresh();
        }

        private void BtnStopRefresh_Click(object sender, RoutedEventArgs e)
        {
            _IsWorketStopped = true;
            BtnStopRefresh.IsEnabled = false;
        }

        private void BtnStartSchedular_Click(object sender, RoutedEventArgs e)
        {
            if (_bgw != null && _bgw.IsBusy)
            {
                MessageBox.Show("Please wait until correct work finished");
            }
            else
                _isTimerEnabled = true;
        }

        private void BtnScheduler_Click(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings()
            {
                Owner = (this)
            };
            settings.ShowDialog();
        }

        private void BtnSet_Click(object sender, RoutedEventArgs e)
        {
            if (TxtRnd.Text.Trim() == string.Empty)
            {
                MessageBox.Show("Enter correct data");
            }
            else
            {
                try
                {
                    _rnd = int.Parse(TxtRnd.Text);
                }
                catch
                {
                    MessageBox.Show("Enter correct data");
                }
            }
        }

        private void BtnViewLog_Click(object sender, RoutedEventArgs e)
        {
            TxtLog.Visibility = Visibility.Visible;
        }

        private void BtnViewResult_Click(object sender, RoutedEventArgs e)
        {
            TxtLog.Visibility = Visibility.Hidden;
        }

        void EnableUI(bool res)
        {
            BtnLoadUser.IsEnabled = res;
            BtnStartRefresh.IsEnabled = res;
            BtnStopRefresh.IsEnabled = !res;
            BtnSet.IsEnabled = res;
            BtnStartSchedular.IsEnabled = res;
            BtnScheduler.IsEnabled = res;
            TxtRnd.IsEnabled = res;
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
            Probar.Minimum = 0.0;
            Probar.Maximum = _refreshDetailsList.Count;
            Probar.Value = 0.0;
            _bgw = new BackgroundWorker();
            _bgw.DoWork += new DoWorkEventHandler(_bgw_DoWork);
            _bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_bgw_RunWorkerCompleted);
            _bgw.RunWorkerAsync();
        }

        void _bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            EnableUI(true);
            _bgw.Dispose();
            if (!_isTimerEnabled)
                return;
            bool flag = false;
            foreach (ScheduleTimer schedularTimer in MainWindow._schedularTimerList)
            {
                if (schedularTimer.From <= DateTime.Now.Hour && schedularTimer.To > DateTime.Now.Hour)
                {
                    _refreshTimer.Interval = new TimeSpan(0, schedularTimer.Interval, 0);
                    flag = true;
                    break;
                }
            }
            if (flag)
            {
                int minutes = _refreshTimer.Interval.Minutes;
                int minValue = minutes - _rnd;
                if (minValue < 0)
                    minValue = 0;
                _refreshTimer.Interval = new TimeSpan(0, new Random().Next(minValue, minutes + _rnd), 0);
                _refreshTimer.IsEnabled = true;
                _refreshTimer.Start();
            }
            else
            {
                _schedulerTimer.IsEnabled = true;
                _schedulerTimer.Start();
            }
        }

        void _bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.Invoke(_updateLogDelegate,"Refreshing process started");

            int i = -1;
            //bool anyErrorAfterLogin = false;
            foreach (var rd in _refreshDetailsList)
            {
                if (_IsWorketStopped)
                    break;
                i++;
                try
                {
                    _cookieCon = null;
                    _cookieCon = new CookieContainer();
                    var htm = GetHtmlWithCookie("http://www.yeeyi.com/bbs/index.php");
                    if (htm.Error)
                    {
                        Dispatcher.Invoke(_UpdateStatusDelegate, htm.Html, i);
                        Dispatcher.Invoke(_updateLogDelegate, "Login page reading error: "+htm.Html);
                        continue;
                    }

                    Dispatcher.Invoke(_updateLogDelegate, "Login page loaded");
                    //File.WriteAllText(@"c:\temp\rMainPage.htm", htm.Html);

                    var formDetails = GetFormDetails(htm.Html);
                    if(formDetails[0]==null||formDetails[1]==null)
                    {
                        Dispatcher.Invoke(_UpdateStatusDelegate, "Error: cant get formhash value.", i);
                        Dispatcher.Invoke(_updateLogDelegate, "Error: cant get formhash value.");
                        continue;
                    }
                    Dispatcher.Invoke(_updateLogDelegate, "formhash value found.");

                    var querry = "formhash="+formDetails[1]+"&referer=&username="+rd.UserName+"&password="+rd.Password+"&loginsubmit=Login";
                    
                    System.Threading.Thread.Sleep(2000);
                    
                    htm = PostRequest(formDetails[0], querry);
                    if(htm.Error)
                    {
                        Dispatcher.Invoke(_UpdateStatusDelegate, htm.Html, i);
                        Dispatcher.Invoke(_updateLogDelegate, rd.UserName + " login failed.\r\nError: " +htm.Html);
                        continue;
                    }

                    //File.WriteAllText(@"c:\temp\rLoggedIn.htm", htm.Html);
                    Dispatcher.Invoke(_updateLogDelegate, rd.UserName + " logged in");
                    System.Threading.Thread.Sleep(2000);

                    htm = GetHtmlWithCookie(rd.Url);

                    if (htm.Error)
                    {
                        Dispatcher.Invoke(_UpdateStatusDelegate, htm.Html, i);
                        Dispatcher.Invoke(_updateLogDelegate, rd.Url + " loading failed.\r\nError: " + htm.Html);
                        //anyErrorAfterLogin = true;
                        continue;
                    }

                    Dispatcher.Invoke(_updateLogDelegate, rd.Url + " loaded");

                    var document = new HtmlAgilityPack.HtmlDocument();
                    document.LoadHtml(htm.Html);

                    var node=document.DocumentNode.SelectSingleNode("//a[@id='k_refresh']");
                    if (node == null)
                    {
                        Dispatcher.Invoke(_UpdateStatusDelegate, "Didnt found refresh option.\nPlease check the login details", i);
                        Dispatcher.Invoke(_updateLogDelegate, "Didnt found refresh option.\nPlease check the login details");
                        //anyErrorAfterLogin = true;
                        continue;
                    }

                    Dispatcher.Invoke(_updateLogDelegate, "Found refresh option");

                    string url = HtmlAgilityPack.HtmlEntity.DeEntitize(node.GetAttributeValue("href", ""));
                    System.Threading.Thread.Sleep(2000);

                    var refreshResult = GetHtmlWithCookie(url);
                    if(refreshResult.Error)
                    {
                        Dispatcher.Invoke(_UpdateStatusDelegate, refreshResult.Html, i);
                        Dispatcher.Invoke(_updateLogDelegate, "Refreshing Error: " +refreshResult.Html);
                        //anyErrorAfterLogin = true;
                        continue;
                    }

                    Dispatcher.Invoke(_updateLogDelegate,rd.Url+" refreshed");

                    //File.WriteAllText(@"c:\temp\rRefresh.htm", htm.Html);

                    System.Threading.Thread.Sleep(2000);

                    htm = GetHtmlWithCookie("http://www.yeeyi.com/bbs/member.php?mod=logging&action=logout&formhash=" + formDetails[1]);

                    if (htm.Error)
                    {
                        Dispatcher.Invoke(_UpdateStatusDelegate, htm.Html, i);
                        Dispatcher.Invoke(_updateLogDelegate, "Logout Error: " + refreshResult.Html);
                        continue;
                    }

                    System.Threading.Thread.Sleep(2000);

                    Dispatcher.Invoke(_UpdateStatusDelegate, "Done", i);
                    Dispatcher.Invoke(_updateLogDelegate, "Logout Success");

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
                UpdateLog(ex.Message);
                UpdateLog("Loading user details failed\nPlease use the correct user details file.");
            }
        }


        
    }
}
