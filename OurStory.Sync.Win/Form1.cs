using System;
using System.Windows.Forms;
using OurStory.Sync.Lib;

namespace OurStory.Sync.Win
{
	public partial class Form1 : Form
    {
        //Bootlegger bootlegger;

        //const string APIKEY = "a736e1500e824ec9ae7558cbed72563d";

        public Form1()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Console.WriteLine("init");
            InitializeComponent();

            Load += Form1_Load;
            FormClosed += Form1_FormClosed;
            
            this.Text = "Indaba Sync v" + Engine.VERSION;
        }

		public void LoadIcon()
		{
			var resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
		}

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Unfortunatly, Indaba Sync has stopped working. Please try again. " + (e.ExceptionObject as Exception).Message);
            Environment.Exit(0);
        }

        void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

		Engine engine;

		void Form1_Load(object sender, EventArgs e)
		{
			engine = new Engine();
			engine.OnSignin += () =>
			{
				BeginInvoke(new Action(() =>
				{
					status.Text = "Connected...";
				}));
			};

			engine.OnUpdateNumbers += (a1, a2, a3) =>
			  {
				  BeginInvoke(new Action(() =>
				  {
					  total.Text = a3.ToString();
					  down.Text = a2.ToString();
					  up.Text = a3.ToString();
				  }));
			  };
			engine.OnSubProgress += (perc) =>
			  {
				  BeginInvoke((Action)delegate ()
				  {
					  progress_sub.Value = perc;
				  });
			  };
			engine.OnProgress += (perc) =>
			  {
				  BeginInvoke((Action)delegate ()
				  {
					  progress.Value = perc;
				  });
			  };
			engine.OnStatusUpdate += (perc) =>
			  {
				  BeginInvoke((Action)delegate ()
				  {
					  status.Text = perc;
				  });
			  };
			engine.OnEnableGo += () =>
			  {
				  BeginInvoke((Action)delegate ()
				  {
					  gobtn.Enabled = true;
				  });
			  };


		}

        

        private void button1_Click(object sender, EventArgs e)
        {
            //var result = folderBrowserDialog1.ShowDialog();
            //if (result == System.Windows.Forms.DialogResult.OK)
            //{
            //    synclocation.Text = folderBrowserDialog1.SelectedPath;
            //}
        }

        string path = "";

        

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				synclocation.Text = dialog.SelectedPath;
				path = dialog.SelectedPath;
				//doit.IsEnabled = false;
				// if (bootlegger.Connected)
				// {
				gobtn.Enabled = false;
				cancelbtn.Enabled = true;
				//transcodechk.Enabled = false;
				applyxmp.Enabled = false;
				engine.SavePath = path;
				//gobtn.Text = "Cancel";
				engine.StartSync();
				//  }
			}
            
        }

        

        private void button3_Click(object sender, EventArgs e)
        {
			//cancel:

			engine.Cancel();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
			//engine.StopAll();
			engine.Cancel();
            if (!engine.IsRunning)
            {
				Environment.Exit(0);
			}
        }

    }
}
