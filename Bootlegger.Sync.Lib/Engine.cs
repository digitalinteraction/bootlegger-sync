using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Bootlegger.Sync.Lib
{
	class Engine
	{
		/*
         * New Engine Code from the Form 
         * 
         */

		public bool IsRunning { get; set; }

		async void scheduler_Tick(object sender, EventArgs e)
		{
			if (!IsRunning)
			{
				await DoRefresh();
				thetotal = 0;
				done = 0;
				exists = 0;
				CalcTotals(allmedia);

				//waiting, done, total
                OnUpdateNumbers?.Invoke(thetotal-exists, exists, thetotal);
				done = exists;

				if (!worker.IsBusy)
					worker.RunWorkerAsync();
			}
		}

        //waiting, done, total 
		public event Action<int, int, int> OnUpdateNumbers;

		JObject allmedia;
		JArray metadata;

		//bool running = false;
		BackgroundWorker worker = new BackgroundWorker();

		void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled)
			{
				Environment.Exit(0);
			}
			else
			{
				IsRunning = false;
				OnStatusUpdate?.Invoke("Up to date at " + DateTime.Now.ToShortTimeString());
			}
		}

		public event Action<string> OnStatusUpdate;

		void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			OnUpdateNumbers?.Invoke((thetotal - done), done, thetotal);
			OnProgress?.Invoke(Math.Min(e.ProgressPercentage, 100));
		}

		int numfiles = 0;

		public string SavePath { get; set; }


		void CalcTotals(JObject dir)
		{
			//make this dir

			if (dir.Properties() != null)
			{
				//var tt = dir.Properties();

				foreach (var t in dir.Properties().ToList())
				{
					if (t.Value != null && !(t.Value is JArray))
					{
						CalcTotals(t.Value as JObject);
					}
					else
					{
						//download files...
						foreach (var f in t.Value as JArray)
						{
							if (f["remote"] != null)
							{
								thetotal++;
								if (SavePath != "")
								{
									if (File.Exists(SavePath + Path.DirectorySeparatorChar + f.Parent.Path.Replace("'].['", Path.DirectorySeparatorChar.ToString()).Replace("['", "").Replace("']", "") + Path.DirectorySeparatorChar + f["local"]))
										exists++;
								}
							}
							numfiles++;

						}
					}
				}
			}
			//for each sub, make that too
		}

		void MakeDir(string p, JObject dir)
		{
			//make this dir

			if (dir.Properties() != null)
			{
				foreach (var t in dir.Properties().ToList())
				{
					Directory.CreateDirectory(p + Path.DirectorySeparatorChar + t.Name);
					string pp = p + Path.DirectorySeparatorChar + t.Name;
					if (t.Value != null && !(t.Value is JArray))
					{
						MakeDir(pp, t.Value as JObject);
					}
					else
					{
						//download files...
						foreach (var f in t.Value as JArray)
						{
							if (f["remote"] != null && !File.Exists(pp + Path.DirectorySeparatorChar + f["local"]))
								DownloadFile(f["remote"].ToString(), pp + Path.DirectorySeparatorChar + f["local"], f["id"].ToString());
						}
					}
				}
			}
			//for each sub, make that too
		}

		//public bool ShouldTranscode { get; set; }
		public bool ShouldApplyXMP { get; set; }

        public async Task DownloadFileInBackground(string src, string dst, CancellationToken cancel)
        {
            WebClient client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Cookie, "sails.sid=" + Uri.EscapeDataString(thesession).Replace("%20", "%2B"));
            cancel.Register(client.CancelAsync);
            Uri uri = new Uri(src);

            // Specify that the DownloadFileCallback method gets called
            // when the download completes.
            //client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCallback2);
            // Specify a progress notification handler.
            client.DownloadProgressChanged += (o, e) =>
            {
                worker.ReportProgress((int)(((done + ((double)e.ProgressPercentage / 100)) / (double)thetotal) * 100));
                OnSubProgress?.Invoke(e.ProgressPercentage);
            };
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            await client.DownloadFileTaskAsync(uri, dst);
        }

        void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Console.WriteLine(e.Error);
        }

        void DownloadFile(string filename, string dest, string id)
		{
			try
			{
				//TODO Change back to bootleggertrans
				//AWSS3Helper helper;

				//if (ShouldTranscode)
					//helper = new AWSS3Helper(Settings.S3ID, Settings.S3KEY, Settings.S3TRANSCODEBUCKET, Settings.S3REGION);
				//else
				//helper = new AWSS3Helper(Settings.S3ID, Settings.S3KEY, Settings.S3BUCKET, Settings.S3REGION);
                
				//meta data for media item
				var themeta = (from n in metadata.Children() where n["id"].ToString() == id select n).First() as JObject;
				//string origpath = "";
				var origpath = themeta["path"].ToString();
			
				if (worker.CancellationPending)
					return;

				try
				{

                    Task.WaitAll(DownloadFileInBackground($"{theport}//{thehostname}/media/full/{id}?apikey={ Settings.APIKEY }", Path.Combine(new FileInfo(dest).DirectoryName, new FileInfo(dest).Name + ".part"),new CancellationToken()));

                    //helper.DownloadFile("upload", origpath +"_homog.mp4", new FileInfo(dest).DirectoryName, new FileInfo(dest).Name + ".part", false, false);

                    File.Move(dest + ".part", dest);
					//write xmp to the file:

					ProcessStartInfo info = new ProcessStartInfo();
					OperatingSystem os = Environment.OSVersion;
					PlatformID pid = os.Platform;
					switch (pid)
					{
						case PlatformID.Win32NT:
						case PlatformID.Win32S:
						case PlatformID.Win32Windows:
						case PlatformID.WinCE:
							info.FileName = "exiftool.exe";
							break;
						case PlatformID.Unix:
							info.FileName = "/usr/local/bin/exiftool";
							break;
						//case PlatformID.MacOSX:
							//info.FileName = "/usr/local/bin/exiftool";
							//break;
						default:
							info.FileName = "exiftool";
							break;
					}

					//info.Arguments = "-xmp:all " + '"' + dest + '"';

					if (ShouldApplyXMP)
					{

						info.Arguments = "-xmp:title=" + '"' + themeta["meta"]["shot_ex"]["name"] + '"' + " ";
						info.Arguments += "-xmp:shotdate=" + '"' + DateTime.Parse(themeta["meta"]["static_meta"]["captured_at"].ToString()).ToString("yyyy:MM:dd HH:mm:ss.ff") + '"' + " ";
						info.Arguments += "-xmp:location=" + '"' + themeta["meta"]["role_ex"]["name"] + '"' + " ";
						info.Arguments += "-xmp:creator=" + '"' + themeta["user"]["profile"]["displayName"] + '"' + " ";
						info.Arguments += "-xmp:subject=" + '"' + themeta["meta"]["coverage_class_ex"]["name"] + '"' + " ";
						info.Arguments += "-xmp:coverage=" + '"' + themeta["meta"]["phase_ex"]["name"] + '"' + " ";
						info.Arguments += "-xmp:gpslatitude=" + '"' + themeta["meta"]["static_meta"]["gps_lat"] + '"' + " ";
						info.Arguments += "-xmp:gpslongitude=" + '"' + themeta["meta"]["static_meta"]["gps_lng"] + '"' + " ";
						if (themeta["meta"]["static_meta"].Contains("edit_tag"))
							info.Arguments += "-xmp:good=true ";

						info.Arguments += '"' + dest + '"';
						Console.WriteLine(info.Arguments);
						info.UseShellExecute = false;
						info.WindowStyle = ProcessWindowStyle.Hidden;
						info.CreateNoWindow = true;
						info.RedirectStandardOutput = true;

						Process p = new Process();
						p.StartInfo = info;

						p.Start();
						//p.OutputDataReceived += p_OutputDataReceived;
						Console.WriteLine(p.StandardOutput.ReadToEnd());

						p.WaitForExit();
					}


				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}

				done++;

				worker.ReportProgress((int)((done / (double)thetotal) * 100));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			Console.WriteLine(e.Data);
		}

		void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			//string illegal = "'?";
			//create all the directories:
			MakeDir(SavePath, allmedia);
			if (worker.CancellationPending)
				e.Cancel = true;
		}

		int thetotal = 0;
		int done = 0;
		private int exists;

		public event Action<int> OnProgress;
		public event Action<int> OnSubProgress;

		//void helper_OnProgress(object sender, Amazon.S3.Model.WriteObjectProgressArgs e)
		//{
  //          worker.ReportProgress((int)(((done + ((double)e.PercentDone / 100)) / (double)thetotal) * 100));
		//	OnSubProgress?.Invoke(e.PercentDone);
		//}

		async Task DoRefresh()
		{
			OnStatusUpdate?.Invoke("Loading Media...");
			RestSharp.RestClient client = new RestSharp.RestClient();
			client.BaseUrl = new Uri(theport + "//" + thehostname);
           
			RestSharp.RestRequest request = new RestSharp.RestRequest("/media/directorystructure/" + theeventid + "?template=" + thetemplate + "&apikey=" + Settings.APIKEY);
			request.AddCookie("sails.sid", Uri.EscapeDataString(thesession).Replace("%20", "%2B"));
			//request.AddHeader("Cookie", "sails.sid=" + thesession);
			var result = await client.ExecuteGetTaskAsync(request);

			var objects = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(result.Content);
			//allmedia = objects as Hashtable;
			allmedia = objects;

			//get all meta-data:
			request = new RestSharp.RestRequest("/media/nicejson/" + theeventid + "?apikey=" + Settings.APIKEY);
			request.AddCookie("sails.sid", Uri.EscapeDataString(thesession).Replace("%20", "%2B"));
			result = await client.ExecuteGetTaskAsync(request);
			metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(result.Content);

			OnStatusUpdate?.Invoke("Ready to Sync.");

            OnUpdateNumbers?.Invoke(0, 0, 0);

			CalcTotals(allmedia);

            OnUpdateNumbers?.Invoke(thetotal-exists, exists, thetotal);

			OnEnableGo?.Invoke();
		}

		public event Action OnEnableGo;

		public void StartSync()
		{
			//start sync:
			if (!IsRunning)
			{
				thetotal = 0;


				CalcTotals(allmedia);
				done = exists;

                OnUpdateNumbers?.Invoke(thetotal-exists, exists, thetotal);
				OnStatusUpdate?.Invoke("Syncing...");

				IsRunning = true;
				scheduler = new System.Timers.Timer();
				scheduler.Elapsed += scheduler_Tick;

				scheduler.Interval = 1000 * 60; //5 min intervals
				scheduler.Start();
				worker.WorkerSupportsCancellation = true;
				worker.DoWork += worker_DoWork;
				worker.ProgressChanged += worker_ProgressChanged;
				worker.RunWorkerCompleted += worker_RunWorkerCompleted;
				worker.WorkerReportsProgress = true;
				worker.RunWorkerAsync();
			}
		}

		public void Cancel()
		{
			scheduler.Stop();
			worker.CancelAsync();
		}

		System.Timers.Timer scheduler = new System.Timers.Timer();

		/*
		 *	Old Engine Code
		 */

		//login to bootlegger if not logged in already:

		public Engine()
		{
			WebServer web = new WebServer(SendResponse, "http://localhost:8664/signin/");
			web.Run();
			ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
			OperatingSystem os = Environment.OSVersion;
			PlatformID pid = os.Platform;
			if (pid == PlatformID.Unix)
			{
				var proc = new ProcessStartInfo("/usr/local/bin/exiftool","-ver");

				proc.RedirectStandardOutput = true;
				proc.RedirectStandardError = true;
				proc.UseShellExecute = false;
				try
				{
					var appexists = Process.Start(proc);
					CanXMP = true;
				}
				catch
				{
					CanXMP = false;
				}	
			}
			else
			{
				CanXMP = true;
			}

		}

		public bool CanXMP { get; set; }

		public event Action OnSignin;

		internal static readonly string VERSION = "1.2";

		string thesession;
		string theeventid;
		string thetemplate;
		string thehostname;
		string theport;
		public async void DoConnnect(string session, string eventid, string templateid, string hostname, string port)
		{
			OnSignin?.Invoke();
			thesession = session;
			theeventid = eventid;
			thetemplate = templateid;
			thehostname = hostname;
			theport = port;
			await DoRefresh();
		}

		public string SendResponse(HttpListenerRequest request)
		{
			DoConnnect(request.QueryString["session"], request.QueryString["eventid"], request.QueryString["template"], request.QueryString["hostname"], request.QueryString["port"]);
			return "";
		}

		class WebServer
		{
			private readonly HttpListener _listener = new HttpListener();
			private readonly Func<HttpListenerRequest, string> _responderMethod;

			public WebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
			{
				if (!HttpListener.IsSupported)
					throw new NotSupportedException(
						"Needs Windows XP SP2, Server 2003 or later.");

				// URI prefixes are required, for example 
				// "http://localhost:8080/index/".
				if (prefixes == null || prefixes.Length == 0)
					throw new ArgumentException("prefixes");

				// A responder method is required
				if (method == null)
					throw new ArgumentException("method");

				foreach (string s in prefixes)
					_listener.Prefixes.Add(s);

				_responderMethod = method;
				_listener.Start();
			}

			public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
				: this(prefixes, method) { }

			public void Run()
			{
				ThreadPool.QueueUserWorkItem((o) =>
				{
					Console.WriteLine("Webserver running...");
					try
					{
						while (_listener.IsListening)
						{
							ThreadPool.QueueUserWorkItem((c) =>
							{
								var ctx = c as HttpListenerContext;
								try
								{

									//var ss = Resources.spacer

									_responderMethod(ctx.Request);
									var assembly = System.Reflection.Assembly.GetExecutingAssembly();
									using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name +".spacer.gif"))
									{
										byte[] buffer = new byte[stream.Length];
										stream.Read(buffer, 0, buffer.Length);
										// TODO: use the buffer that was read
										ctx.Response.ContentLength64 = buffer.Length;
										ctx.Response.ContentType = "image/gif";
										ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
									}



								}
								catch (UriFormatException ee)
								{
									throw ee;
								}
								catch (Exception)
								{

								} // suppress any exceptions
								finally
								{
									// always close the stream
									ctx.Response.OutputStream.Close();
								}
							}, _listener.GetContext());
						}
					}
					catch { } // suppress any exceptions
				});
			}

			public void Stop()
			{
				_listener.Stop();
				_listener.Close();
			}
		}
	}


}