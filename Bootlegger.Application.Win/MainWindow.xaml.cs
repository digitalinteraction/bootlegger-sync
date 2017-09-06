using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Initialized;
        }

        private async void MainWindow_Initialized(object sender, EventArgs e)
        {
            //show progress...

            progress.Visibility = Visibility.Visible;
            try
            {
                await App.BootleggerApp.Start();
            }
            catch
            {
                MessageBox.Show("The connection to Docker has timed out, please restart docker manually.");
                Environment.Exit(1);
            }
            progress.Visibility = Visibility.Hidden;

            switch (App.BootleggerApp.CurrentState)
            {
                case Lib.BootleggerApplication.RUNNING_STATE.NOT_SUPORTED:
                    //close with error
                    MessageBox.Show("This OS is not supported, please try on another system");
                    Environment.Exit(1);
                    break;

                case Lib.BootleggerApplication.RUNNING_STATE.NO_DOCKER:
                case Lib.BootleggerApplication.RUNNING_STATE.NO_IMAGES:
                case Lib.BootleggerApplication.RUNNING_STATE.NO_DOCKER_RUNNING:
                    _mainFrame.Content = new Intro();
                    break;
                case Lib.BootleggerApplication.RUNNING_STATE.READY:
                    //show status (logs, window to open, location of directory of videos)
                    _mainFrame.Content = new Running();
                    break;
            }



            
        }
    }
}
