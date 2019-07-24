using System;
using System.IO;
using System.Reflection;
using System.Linq;

namespace OurStory.Sync.Lib
{
    public class Settings
    {
        static Settings()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("APIKEY",StringComparison.InvariantCulture));
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                APIKEY = reader.ReadToEnd();
            }
        }

        public static string APIKEY;
    }
}
