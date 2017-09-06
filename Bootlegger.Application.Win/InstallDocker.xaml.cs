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
    /// Interaction logic for InstallDocker.xaml
    /// </summary>
    public partial class InstallDocker
    {
        public InstallDocker()
        {
            InitializeComponent();
            Loaded += InstallDocker_Loaded;
        }

        private void InstallDocker_Loaded(object sender, RoutedEventArgs e)
        {
            href.Inlines.Add(new Run(App.BootleggerApp.DockerLink));
        }

        private void continuebtn_Copy_Click(object sender, RoutedEventArgs e)
        {
            //back
            (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Intro();
        }

        private async void continuebtn_Click(object sender, RoutedEventArgs e)
        {
            //only enable when docker is installed and running...
            await App.BootleggerApp.CheckDocker();
            if (App.BootleggerApp.CurrentState == Lib.BootleggerApplication.RUNNING_STATE.NO_IMAGES || App.BootleggerApp.CurrentState == Lib.BootleggerApplication.RUNNING_STATE.READY)
            {
                //continue
                (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new DownloadImages();
            }
            else
            {
                MessageBox.Show("Check Docker is running and try again");
            }
        }

        private void href_Click(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OpenDownloadLink();
        }
    }
}
