using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Form_Refresh
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
        }

        List<ScheduleTimer> _scheduler=new List<ScheduleTimer>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _scheduler = new List<ScheduleTimer>(MainWindow._schedularTimerList);
            DgTimer.ItemsSource = _scheduler;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            MainWindow._schedularTimerList = (List<ScheduleTimer>)null;
            MainWindow._schedularTimerList = new List<ScheduleTimer>((IEnumerable<ScheduleTimer>)_scheduler);
            try
            {
                string contents = "";
                foreach (ScheduleTimer schedularTimer in MainWindow._schedularTimerList)
                    contents = contents + (object)schedularTimer.From + "," + (object)schedularTimer.To + "," + (object)schedularTimer.Interval + "\n";
                File.WriteAllText("sch.txt", contents);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DgTimer.SelectedItems.Count <= 0)
                    return;
                foreach (ScheduleTimer selectedItem in DgTimer.SelectedItems)
                    _scheduler.Remove(selectedItem);
                DgTimer.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (TxtFrom.Text.Trim() == string.Empty)
            {
                MessageBox.Show("Please enter correct data");
            }
            else if (TxtInterval.Text.Trim() == string.Empty)
            {
                MessageBox.Show("Please enter correct data");
            }
            else if (TxtTo.Text.Trim() == string.Empty)
            {
                MessageBox.Show("Please enter correct data");
            }
            else
            {
                int to = 0;
                int from = 0;
                int num4;
                try
                {
                    to = int.Parse(TxtTo.Text);
                    from = int.Parse(TxtFrom.Text);
                    num4 = int.Parse(TxtInterval.Text);
                }
                catch
                {
                    MessageBox.Show("Please enter correct data");
                    return;
                }
                if (_scheduler.Any<ScheduleTimer>((Func<ScheduleTimer, bool>)(x =>
                {
                    if (x.To != to)
                        return x.From == from;
                    return true;
                })))
                {
                    MessageBox.Show("Data already exists please enter different data");
                }
                else
                {
                    _scheduler.Add(new ScheduleTimer()
                    {
                        To = to,
                        From = from,
                        Interval = num4
                    });
                    DgTimer.Items.Refresh();
                }
            }
        }
    }
}
