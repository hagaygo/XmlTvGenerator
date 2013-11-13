using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XmlTvGenerator.Core;

namespace XmlTvGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = Config.GetInstance();

            List<Show> shows = GetShows();
            var xmltv = new XmlTv();
            using (var f = new FileStream(config.OutputPath, FileMode.OpenOrCreate))
                xmltv.Save(shows, f);
        }

        private static List<Show> GetShows()
        {
            var config = Config.GetInstance();
            var lst = new List<Show>();

            foreach (var grabber in config.Grabbers)
            {
                var a = Assembly.LoadFrom(grabber.Path);
                foreach (var t in a.GetTypes())
                    if (t.IsSubclassOf(typeof(GrabberBase)))
                    {
                        var gb = (GrabberBase)Activator.CreateInstance(t);
                        var shows = gb.Grab(grabber.Parameters);
                        if (!string.IsNullOrEmpty(grabber.ChannelPrefix))
                            shows.ForEach(x => x.Channel = grabber.ChannelPrefix + x.Channel);
                        lst.AddRange(shows);
                    }
            }

            return lst;
        }        
    }
}
