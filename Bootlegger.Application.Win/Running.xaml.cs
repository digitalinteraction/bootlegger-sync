using Bootlegger.App.Lib;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace Bootlegger.App.Win
{
    /// <summary>
    /// Interaction logic for Running.xaml
    /// </summary>
    public partial class Running
    {
        public Running()
        {
            InitializeComponent();
            Loaded += Running_Loaded;
        }

        private async void Running_Loaded(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OnLog += BootleggerApp_OnLog;
            progress.Content = "Starting application...";
            //App.BootleggerApp.CreateWiFi("bootlegger","coolshot");
            //ssid.Content = $"http://{BootleggerApplication.GetLocalIPAddress()}";
            //ssid.Content = "SSID: bootlegger";
            //pwd.Content = "";
            if (await App.BootleggerApp.RunServer())
            {
                progress.Content = "Running";
            }
            else
            {
                progress.Content = "Problem starting application!";
            }
        }

        private void BootleggerApp_OnLog(string obj)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                obj = Regex.Replace(obj, @"[^\u0020-\u007F]+", string.Empty);
                if (obj.Length > 0)
                {
                    log.AppendText($"{DateTime.Now.ToShortTimeString()} - {obj.Trim('\n', '\r')}\r");
                    log.ScrollToEnd();
                }
            }));
        }

        private void continuebtn_Click(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OpenAdminPanel();
        }

        private void continuebtn_Copy_Click(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OpenFolder();
        }

        private void continuebtn_Copy1_Click(object sender, RoutedEventArgs e)
        {
            //update
            (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new DownloadImages(true);
        }

        private async void backupbtn_Click(object sender, RoutedEventArgs e)
        {
            backupbtn.IsEnabled = false;
            await App.BootleggerApp.BackupDatabase();
            backupbtn.IsEnabled = true;
        }

        private async void restorebtn_Click(object sender, RoutedEventArgs e)
        {
            var diag = new Avalon.Windows.Dialogs.FolderBrowserDialog() { BrowseFiles = false };
            var folder = diag.ShowDialog();

            if (diag.SelectedPath != null)
            {
                restorebtn.IsEnabled = false;
                await App.BootleggerApp.RestoreDatabase(diag.SelectedPath);
                restorebtn.IsEnabled = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (App.Current.MainWindow as MetroWindow).Close();
        }
    }
}
