using System;
using System.Collections.Generic;
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
            progress.Content = "Starting application...";
            App.BootleggerApp.CreateWiFi("bootlegger","coolshot");
            ssid.Content = "SSID: bootlegger";
            pwd.Content = "Password: coolshot";
            if (await App.BootleggerApp.RunServer())
            {
                progress.Content = "Running";
            }
            else
            {
                progress.Content = "Problem starting application!";
            }
        }

        private void continuebtn_Click(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OpenAdminPanel();
        }

        private void continuebtn_Copy_Click(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OpenFolder();
        }

        private async void continuebtn_Copy1_Click(object sender, RoutedEventArgs e)
        {
            //update
            (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new DownloadImages(true);
        }
    }
}
