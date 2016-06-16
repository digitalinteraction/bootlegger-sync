using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ElasticTranscoder.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using S3Downloader;

namespace SyncTray
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

				OnUpdateNumbers?.Invoke(exists, thetotal,0);
				done = exists;

				if (!worker.IsBusy)
					worker.RunWorkerAsync();
			}
		}

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
				//gobtn.Enabled = true;
				//cancelbtn.Enabled = false;
				OnStatusUpdate?.Invoke("Up to date at " + DateTime.Now.ToShortTimeString());
				//status.Text = ";
				//MessageBox.Show("Download Complete");
				//button2.Enabled = true;
			}
		}

		public event Action<string> OnStatusUpdate;

		void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			OnUpdateNumbers?.Invoke(thetotal, done,(thetotal - done));
			////total.Text = thetotal.ToString();
			////down.Text = done.ToString();
			//up.Text = (thetotal - done).ToString();
			//progress.Value = Math.Min(e.ProgressPercentage, 100);
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
					//Directory.CreateDirectory(p + Path.DirectorySeparatorChar + t.Name);
					//string pp = p + Path.DirectorySeparatorChar + t.Name;
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
				//var tt = dir.Properties();

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

		public bool ShouldTranscode { get; set; }
		public bool ShouldApplyXMP { get; set; }

		void DownloadFile(string filename, string dest, string id)
		{

			try
			{
				//TODO Change back to bootleggertrans
				AWSS3Helper helper;



				if (ShouldTranscode)
					helper = new AWSS3Helper(Settings.S3ID, Settings.S3KEY, Settings.S3TRANSCODEBUCKET, Settings.S3REGION);
				else
					helper = new AWSS3Helper(Settings.S3ID, Settings.S3KEY, Settings.S3BUCKET, Settings.S3REGION);



				//AWSS3Helper transcodehelper = new AWSS3Helper("AKIAJJJ5WKR46X6URJNA", "HBDdh9nYI8EajSfeyvdMwiXjO66T2YPZi3g3xPLb", "bootleggertrans", Amazon.RegionEndpoint.EUWest1);

				//meta data for media item
				var themeta = (from n in metadata.Children() where n["id"].ToString() == id select n).First() as JObject;
				//string origpath = "";
				var origpath = themeta["path"].ToString();
				//if (filename.Split('/').Count() > 0)
				//    origpath = filename.Split('/').Last();

				/** INIT HOMOG IF NEEDED: **/
				if (themeta["meta"]["static_meta"]["media_type"].ToString() == "VIDEO")
				{

					//replace normal url with transcode url

					// PUT IN TO QUICKLY AVOID HOMOG -- NEEDS FIXING!
					if (ShouldTranscode)
					{
						origpath += "_homog.mp4";

						RestRequest req = new RestRequest("/media/homog/" + id, RestSharp.Method.HEAD);
						RestClient client = new RestClient();

						client.FollowRedirects = true;
						client.BaseUrl = new Uri(theport + "//" + thehostname);
						req.AddCookie("sails.sid", Uri.EscapeDataString(thesession).Replace("%20", "%2B"));
						var res = client.Execute(req);
						if (res.StatusCode != HttpStatusCode.OK)
						{
							//make homog
							RestSharp.RestRequest request = new RestSharp.RestRequest("/media/transcodefile/?filename=" + Uri.EscapeDataString(origpath) + "&apikey=" + Settings.APIKEY);
							request.AddCookie("sails.sid", Uri.EscapeDataString(thesession).Replace("%20", "%2B"));
							var job = client.Execute(request);

							var interim = JsonConvert.DeserializeObject<Hashtable>(job.Content);
							if (interim.ContainsKey("jobid"))
							{
								var jobresult = interim["jobid"].ToString();

								Amazon.ElasticTranscoder.AmazonElasticTranscoderClient elastic = new Amazon.ElasticTranscoder.AmazonElasticTranscoderClient(Settings.S3ID, Settings.S3KEY, Settings.S3REGION);

								bool done = false;
								while (!done)
								{
									Thread.Sleep(2000);
									Task<ReadJobResponse> task = elastic.ReadJobAsync(new ReadJobRequest() { Id = jobresult });
									task.Wait();
									var j = task.Result;
									//ReadJobResponse j = Task<ReadJobResponse>.WaitAll(task);
									//var j = elastic.ReadJob(new ReadJobRequest() { Id = jobresult });
									if (j.Job.Status == "Complete" || j.Job.Status == "Error")
									{
										Console.WriteLine(j.Job.Status);
										done = true;
									}
								}
							}
							else
							{
								//failed to create transcode...
							}
						}
						else
						{
							Console.WriteLine("transcoded file exists");
						}
					}
				}

				//int total = (from n in allmedia where n.ContainsKey("path") select n).Count();
				//int count = 0;
				//foreach (Hashtable media in (from n in allmedia where n.ContainsKey("path") select n))
				//{
				if (worker.CancellationPending)
					return;

				try
				{

					helper.OnProgress += helper_OnProgress;
					if (ShouldTranscode)
						helper.DownloadFile("upload", themeta["path"].ToString() + "_homog.mp4", new FileInfo(dest).DirectoryName, new FileInfo(dest).Name + ".part", false, true);
					else
						helper.DownloadFile("upload", themeta["path"].ToString(), new FileInfo(dest).DirectoryName, new FileInfo(dest).Name + ".part", false, true);

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
							info.FileName = "exiftool";
							break;
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

				//}
				//count++;
				//BeginInvoke((Action)delegate(){
				//    up.Text = (total - count).ToString();
				//    down.Text = count.ToString();
				//});

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

		void helper_OnProgress(object sender, Amazon.S3.Model.WriteObjectProgressArgs e)
		{
			OnSubProgress?.Invoke(e.PercentDone);
		}


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
			//status.Text = "Ready to Sync.";
			//up.Content = objects.Count;

			OnUpdateNumbers(0, 0, 0);
			//up.Text = "-";
			//down.Text = "-";
			//thetotal = 0;
			CalcTotals(allmedia);
			//total.Text = thetotal.ToString();
			//up.Text = thetotal.ToString();
			OnUpdateNumbers(thetotal, 0, thetotal);

			//gobtn.Enabled = true;
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
				//total.Text = thetotal.ToString();
				//down.Text = exists.ToString();
				OnUpdateNumbers?.Invoke(thetotal, 0, exists);

				IsRunning = true;
				scheduler = new System.Timers.Timer();
				scheduler.Elapsed += scheduler_Tick;
				//scheduler.Tick += scheduler_Tick;
				//scheduler.Enabled = true;
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
		}

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

									string rstr = _responderMethod(ctx.Request);
									//byte[] buf = Encoding.UTF8.GetBytes(rstr);
									var assembly = System.Reflection.Assembly.GetExecutingAssembly();
									using (var stream = assembly.GetManifestResourceStream("SyncTray.spacer.gif"))
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
