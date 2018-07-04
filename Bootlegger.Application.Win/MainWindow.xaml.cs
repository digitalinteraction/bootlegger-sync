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
        bool closing = false;

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closing)
            {
                e.Cancel = true;
                return;
            }



            if (!canexit)
            {
                e.Cancel = true;
                var tt = await (App.Current.MainWindow as MetroWindow).ShowMessageAsync("Continue?", "Closing this application will prevent access to Our Story", MessageDialogStyle.AffirmativeAndNegative);
                if (tt == MessageDialogResult.Affirmative)
                {
                    closing = true;
                    _mainFrame.Visibility = Visibility.Collapsed;
                    progress.Visibility = Visibility.Visible;
                    await App.BootleggerApp.StopServer();
                    canexit = true;
                    closing = false;
                    Close();
                }
            }


            //if (!canexit)
            //{
                
            //}
            //else
            //{
                
            //    //App.BootleggerApp.StopWifi();
            //}
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


            //await App.BootleggerApp.DownloadImages(false,new CancellationTokenSource().Token);

            //App.BootleggerApp.CurrentState = Lib.BootleggerApplication.RUNNING_STATE.NOT_SUPPORTED;

            switch (App.BootleggerApp.CurrentState)
            {
                case Lib.BootleggerApplication.RUNNING_STATE.NOT_SUPPORTED:
                case Lib.BootleggerApplication.RUNNING_STATE.NO_DOCKER:
                case Lib.BootleggerApplication.RUNNING_STATE.NO_IMAGES:
                case Lib.BootleggerApplication.RUNNING_STATE.NO_DOCKER_RUNNING:
                case Lib.BootleggerApplication.RUNNING_STATE.NOWIFICONFIG:
                    _mainFrame.Content = new Checklist();
                    break;
                case Lib.BootleggerApplication.RUNNING_STATE.READY:
                    //show status (logs, window to open, location of directory of videos)
                    //_mainFrame.Content = new DownloadImages();
                    _mainFrame.Content = new Running();
                    break;
            }



            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //open help:
            App.BootleggerApp.OpenDocs();
        }
    }
}
