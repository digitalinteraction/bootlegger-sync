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
using MahApps.Metro.Controls.Dialogs;

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
            Closing += MainWindow_Closing;
            MouseDown += Window_MouseDown;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        bool canexit = false;

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!canexit)
            {
                e.Cancel = true;
                var tt = await (App.Current.MainWindow as MetroWindow).ShowMessageAsync("Continue?", "Closing this application will prevent access to Bootlegger", MessageDialogStyle.AffirmativeAndNegative);
                if (tt == MessageDialogResult.Affirmative)
                {
                    canexit = true;
                    Close();
                }
                else
                    canexit = false;
            }
            else
            {
                await App.BootleggerApp.StopServer();
                App.BootleggerApp.StopWifi();
            }
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
                await (App.Current.MainWindow as MetroWindow).ShowMessageAsync("Message", "The connection to Docker has timed out, please restart docker manually.");
                //MessageBox.Show("The connection to Docker has timed out, please restart docker manually.");
                Environment.Exit(1);
            }
            progress.Visibility = Visibility.Hidden;

            switch (App.BootleggerApp.CurrentState)
            {
                case Lib.BootleggerApplication.RUNNING_STATE.NOT_SUPORTED:
                    //close with error
                    await (App.Current.MainWindow as MetroWindow).ShowMessageAsync("Message", "This OS is not supported, please try on another system");
                    //MessageBox.Show("This OS is not supported, please try on another system");
                    Environment.Exit(1);
                    break;

                case Lib.BootleggerApplication.RUNNING_STATE.NO_DOCKER:
                case Lib.BootleggerApplication.RUNNING_STATE.NO_IMAGES:
                case Lib.BootleggerApplication.RUNNING_STATE.NO_DOCKER_RUNNING:
                    _mainFrame.Content = new Intro();
                    break;
                case Lib.BootleggerApplication.RUNNING_STATE.READY:
                    //show status (logs, window to open, location of directory of videos)
                    //_mainFrame.Content = new DownloadImages();
                    _mainFrame.Content = new Running();
                    break;
            }



            
        }
    }
}
