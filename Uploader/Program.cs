using Bootlegger.Sync.Lib;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uploader
{
    class Program
    {

        static async Task Start()
        {
            try
            {
                //get list of files in the shoot

                //login:

                //get the list of directories:
                RestSharp.RestClient client = new RestSharp.RestClient();
                client.BaseUrl = new Uri("https://ifrc.bootlegger.tv");


                //RestSharp.RestRequest request = new RestSharp.RestRequest("/media/directorystructure/" + theeventid + "?template=" + thetemplate + "&apikey=" + Settings.APIKEY);
                //request.AddCookie("sails.sid", Uri.EscapeDataString(thesession).Replace("%20", "%2B"));
                ////request.AddHeader("Cookie", "sails.sid=" + thesession);
                //var result = await client.ExecuteGetTaskAsync(request);

                //var objects = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(result.Content);
                //allmedia = objects as Hashtable;
                //allmedia = objects;

                //get all meta-data:

                RestRequest request = new RestSharp.RestRequest("/media/nicejson/599aabf6cb8cf02100ded037?apikey=" + Settings.APIKEY);
                request.AddCookie("sails.sid", "s:A4ddVa9aGjD_gLy3w6asyOU0.hdgxX7J6AQwaJ7xRDAjE3%2FXxA7anmVRf2FCBSI3YJCY");//.Replace(" %20", "%2B");
                var result = await client.ExecuteGetTaskAsync(request);
                var metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(result.Content);

                var notuploaded = from n in metadata where n["path"] == null select n;

                List <string> files = new List<string>();
                DirSearch(@"E:\Berau_Bootlegger_Video_Dumps\Day3_28082017\", files);
                Console.WriteLine("Found " + files.Count + " found");
                Console.WriteLine("Found " + metadata.Count() + " online");
                Console.WriteLine(notuploaded.Count() + " not uploaded");

                foreach (var file in files)
                {
                    FileInfo ff = new FileInfo(file);
                    var filename = ff.Name;
                    var group = ff.Directory.Name;

                    Console.WriteLine("Processing " + ff.Name);

                    //find in metadata:
                    var found = from n in metadata where n["path"].ToString() == filename select n;

                    if (found != null)
                    {
                        Console.WriteLine("Found online");
                        //get signed url

                        //push to s3

                        //notify
                    }
                    else
                    {
                        Console.WriteLine("Not found online");
                    }
                }


                Console.WriteLine("jeff");

                //get the list of files

                //for each file -- if it matches a filename in the list, upload it.
            }
            catch (Exception e)
            {

            }
        }

        static void DirSearch(string sDir, List<string> files)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        files.Add(f);
                    }
                    DirSearch(d, files);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        static void Main(string[] args)
        {
            Task.WaitAll(Start());
        }
    }
}
