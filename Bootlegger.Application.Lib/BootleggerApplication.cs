using Docker.DotNet;
using Docker.DotNet.Models;
using Rssdp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace Bootlegger.App.Lib
{
    public class BootleggerApplication:IProgress<JSONMessage>
    {
        public enum RUNNING_STATE { NOT_SUPORTED, NO_DOCKER, NO_DOCKER_RUNNING, NO_IMAGES, READY, RUNNING }

        public RUNNING_STATE CurrentState { get; private set; }
        public OperatingSystem CurrentPlatform { get; private set; }

        public BootleggerApplication()
        {
            CurrentPlatform = System.Environment.OSVersion;
        }

        Docker.DotNet.DockerClient dockerclient;

        private SsdpDevicePublisher _Publisher;

        public static string GetLocalIPAddress()
        {
            //return Dns.GetHostName();

            return "10.10.10.1";

            //var host = Dns.GetHostEntry(Dns.GetHostName());
            //foreach (var ip in host.AddressList)
            //{
            //    if (ip.AddressFamily == AddressFamily.InterNetwork)
            //    {
            //        return ip.ToString();
            //    }
            //}
            //throw new Exception("Local IP Address Not Found!");
        }

        public async Task Start()
        {
            _Publisher = new SsdpDevicePublisher();
            var deviceDefinition = new SsdpRootDevice()
            {
                CacheLifetime = TimeSpan.FromMinutes(30), //How long SSDP clients can cache this info.
                Location = new Uri("http://"+ GetLocalIPAddress()), // Must point to the URL that serves your devices UPnP description document. 
                DeviceTypeNamespace = "bootlegger",
                DeviceType = "server",
                DeviceVersion = 1,
                FriendlyName = "Bootlegger Server",
                Manufacturer = "Newcastle University",
                ModelName = "Serverv1",
                Uuid = "30f4d4fe-59e6-11e8-9c2d-fa7ae01bbebc" // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.
            };
            _Publisher.NotificationBroadcastInterval = TimeSpan.FromSeconds(10);
            _Publisher.AddDevice(deviceDefinition);
            _Publisher.StandardsMode = SsdpStandardsMode.Relaxed;

            if (CurrentPlatform.Platform == PlatformID.Win32Windows || CurrentPlatform.Platform == PlatformID.Unix)
            {
                CurrentState = RUNNING_STATE.NOT_SUPORTED;
                return;
            }

            /////FOR DEBUG
            //CurrentState = RUNNING_STATE.RUNNING;
            //return;


            try
            { 
                await CheckDocker();
            }
            catch (Exception e)
            {
                CurrentState = RUNNING_STATE.NO_DOCKER;
            }

            //CurrentState = RUNNING_STATE.NO_DOCKER;

            await Task.Run(() =>
            {
                //start docker connection
                switch (CurrentPlatform.Platform)
                {
                    case PlatformID.Win32NT:
                        dockerclient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
                        break;
                }
            });

            if (CurrentState == RUNNING_STATE.NO_IMAGES)
            {
                
                Task containers = dockerclient.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters() { All = true });
                if (await Task.WhenAny(containers, Task.Delay(10000)) == containers)
                {
                    //containers installed?
                    try
                    {
                        var exists = await dockerclient.Images.InspectImageAsync("bootlegger/server-app");
                        CurrentState = RUNNING_STATE.READY;
                    }
                    catch (Exception e)
                    {
                        CurrentState = RUNNING_STATE.NO_IMAGES;
                    }
                }
                else
                {
                    // timeout logic
                    CurrentState = RUNNING_STATE.NO_DOCKER_RUNNING;
                }                
            }
        }

        internal void OpenDownloadLink()
        {
            System.Diagnostics.Process.Start(DockerLink);
        }

        public async Task CheckDocker()
        {
            await Task.Run(() =>
            {
                Process p = new Process();
                p.StartInfo = new ProcessStartInfo()
                {
                    FileName = "docker",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                p.Start();
                p.WaitForExit();

                p = new Process();
                p.StartInfo = new ProcessStartInfo()
                {
                    FileName = "docker-compose",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                p.Start();
                p.WaitForExit();

                p = new Process();
                p.StartInfo = new ProcessStartInfo()
                {
                    FileName = "docker",
                    Arguments = "info",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                p.Start();
                p.WaitForExit();
                if (p.ExitCode == 0)
                    CurrentState = RUNNING_STATE.NO_IMAGES;
                else
                {
                    CurrentState = RUNNING_STATE.NO_DOCKER_RUNNING;
                }
            });
        }

        internal void OpenFolder()
        {
            System.Diagnostics.Process.Start(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "upload");
        }

        public string DockerLink
        {
            get
            {
                switch (CurrentPlatform.Platform)
                {
                    case PlatformID.Win32NT:
                        return "https://store.docker.com/editions/community/docker-ce-desktop-windows";
                    case PlatformID.MacOSX:
                        return "https://store.docker.com/editions/community/docker-ce-desktop-mac";
                    default:
                        return "https://store.docker.com";
                }
            }
        }

        public void OpenAdminPanel()
        {
            System.Diagnostics.Process.Start("http://localhost");
        }

        public void OpenDocs()
        {
            System.Diagnostics.Process.Start("https://our-story.gitbook.io/standalone");
        }

        List<ImagesCreateParameters> imagestodownload;
        private int CurrentDownload = 0;

        public async Task DownloadImages(bool forceupdate, CancellationToken cancel)
        {
            CurrentDownload = 1;

            imagestodownload = new List<ImagesCreateParameters>();

            //load from yaml:
            var Document = File.ReadAllText("docker-compose.yml");
            var input = new StringReader(Document);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);
            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

            foreach (var entry in mapping.Children)
            {
                if ((entry.Key as YamlScalarNode).Value == "services")
                {
                    foreach (var service in (entry.Value as YamlMappingNode).Children)
                    {
                        //Console.WriteLine(service.Key);
                        var key = new YamlScalarNode("image");
                        var image = ((service.Value as YamlMappingNode).Children[key] as YamlScalarNode).Value;
                        var img = image.Split(':');
                        imagestodownload.Add(new ImagesCreateParameters()
                        { 
                            FromImage = img[0],
                            Tag = (img.Length > 1)?img[1] : "latest"
                        });
                        //Console.WriteLine(image);
                    }
                    //output.WriteLine(((YamlScalarNode)entry.Key).Value);
                }
            }

            List<Task> tasks = new List<Task>();

            foreach(var im in imagestodownload)
            {
                if (!cancel.IsCancellationRequested)
                {
                    Layers.Clear();

                    //detect if it exists:
                    try
                    {
                        if (forceupdate)
                            throw new Exception("Must update");
                        var exists = await dockerclient.Images.InspectImageAsync(im.FromImage, cancel);
                        //CurrentDownload++;
                        //OnNextDownload(CurrentDownload, imagestodownload.Count, CurrentDownload / (double)imagestodownload.Count);
                    }
                    catch
                    {
                        await dockerclient.Images.CreateImageAsync(im, null, this, cancel);
                        //CurrentDownload++;
                    }
                    finally
                    {
                        CurrentDownload++;
                        OnNextDownload(CurrentDownload, imagestodownload.Count, CurrentDownload / (double)imagestodownload.Count);
                    }
                }
                else
                    break;
            }
        }


        //private async Task StartContainer(CreateContainerParameters param)
        //{
        //    try
        //    {
        //        //check if containers are running:
        //        var spec = await dockerclient.Containers.InspectContainerAsync(param.Name);
        //        await dockerclient.Containers.StartContainerAsync(spec.ID, null);
        //    }
        //    catch
        //    {
        //        await dockerclient.Containers.CreateContainerAsync(param);
        //    }
        //}

        //private async Task<VolumeResponse> CreateVolume(VolumesCreateParameters param)
        //{
        //    try
        //    {
        //        return await dockerclient.Volumes.InspectAsync(param.Name);
        //    }
        //    catch
        //    {
        //        return await dockerclient.Volumes.CreateAsync(param);
        //    }
        //}

        public event Action<string> OnLog;

        Process currentProcess;

        //start containers...
        public async Task<bool> RunServer()
        {
            try
            {

                if (currentProcess != null && !currentProcess.HasExited)
                {
                    currentProcess.Close();
                }
                //if not running:
                currentProcess = new Process();
                currentProcess.StartInfo = new ProcessStartInfo("docker-compose");
                currentProcess.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                currentProcess.StartInfo.Arguments = "-p bootleggerlocal up -d";
                currentProcess.StartInfo.Environment.Add("MYIP", GetLocalIPAddress());
                currentProcess.StartInfo.UseShellExecute = false;
                currentProcess.StartInfo.CreateNoWindow = true;
                currentProcess.StartInfo.RedirectStandardOutput = true;
                currentProcess.StartInfo.RedirectStandardError = true;

                currentProcess.Start();
                currentProcess.BeginOutputReadLine();
                currentProcess.BeginErrorReadLine();

                currentProcess.OutputDataReceived += (s, o) =>
                {
                    if (o.Data != null)
                        OnLog?.Invoke(o.Data.Trim());
                };

                currentProcess.ErrorDataReceived += (s, o) =>
                {
                    if (o.Data != null)
                        OnLog?.Invoke(o.Data.Trim());
                };

                await Task.Run(() =>
                {
                    currentProcess.WaitForExit();
                });

                WebClient client = new WebClient();

                bool connected = false;
                int count = 0;
                while (!connected && count < 3)
                {
                    try
                    {
                        var result = await client.DownloadStringTaskAsync($"http://{GetLocalIPAddress()}/status");
                        connected = true;
                    }
                    catch
                    {
                        await Task.Delay(5000);
                    }
                    finally
                    {
                        count++;
                    }
                }


                return connected;
            }
            catch
            {
                return false;
            }
        }

        //stop containers
        public async Task StopServer()
        {
            if (currentProcess != null && !currentProcess.HasExited)
            {
                currentProcess.Close();
            }
            

            try
            {
                currentProcess = new Process();
                currentProcess.StartInfo = new ProcessStartInfo("docker-compose");
                currentProcess.StartInfo.Arguments = "-p bootleggerlocal stop";
                currentProcess.StartInfo.UseShellExecute = false;
                currentProcess.StartInfo.CreateNoWindow = true;
                currentProcess.Start();
                await Task.Run(() =>
                {
                    currentProcess.WaitForExit();
                });
            }
            catch
            {
                //cant stop?
            }
        }

        //message, current, total, sub, overall
        public event Action<string, int, int, Dictionary<string, double>, double> OnDownloadProgress;
        public event Action<int, int, double> OnNextDownload;

        Dictionary<string, double> Layers = new Dictionary<string, double>();

        public void Report(JSONMessage value)
        {
            //Debug.WriteLine(value.From);

            //Debug.WriteLine(value.Status);
            //Debug.WriteLine(value.ProgressMessage);
            if (value.ProgressMessage != null)
            {
                //Debug.WriteLine(value.ProgressMessage);
                Layers[value.ID] = value.Progress.Current / (double)value.Progress.Total;
                //Console.WriteLine(CurrentDownload);
                OnDownloadProgress?.Invoke(value.Status, CurrentDownload, imagestodownload.Count, Layers, CurrentDownload / (double)imagestodownload.Count);
            }
        }
    }
}
