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
        static ILogger Logger;

        static void Main(string[] args)
        {
            var config = Config.GetInstance();
            if (config.LoggerType != null)
                Logger = GetLogger(config);
            else
                Logger = new XmlTvGenerator.Logger.DummyLogger();
            Logger.WriteEntry("Grabbing started", LogType.Info);
            try
            {
                List<Show> shows = GetShows();
                if (shows.Count == 0)
                    Console.WriteLine("No shows found.");
                else
                {
                    var xmltv = new XmlTv();
                    using (var f = new FileStream(config.OutputPath, FileMode.Truncate))
                        xmltv.Save(shows, f);
                }
                Logger.WriteEntry("Grabbing finished", LogType.Info);
            }
            catch (Exception e)
            {
                Logger.WriteEntry(e.Message, LogType.Error);
            }
        }

        private static ILogger GetLogger(Config config)
        {
            var log = (ILogger)Activator.CreateInstance(Type.GetType("XmlTvGenerator.Logger." + config.LoggerType + "Logger"));
            log.InitLog(config.LoggerParameters);
            return log;
        }

        private static List<Show> GetShows()
        {
            var config = Config.GetInstance();
            var lst = new List<Show>();
            if (config.Grabbers.Count == 0)
                Console.WriteLine("No grabbers defined in config");
            else
                foreach (var grabber in config.Grabbers)
                {
                    var a = Assembly.LoadFrom(grabber.Path);
                    foreach (var t in a.GetTypes())
                        if (t.IsSubclassOf(typeof(GrabberBase)))
                        {
                            var gb = (GrabberBase)Activator.CreateInstance(t);
                            var shows = gb.Grab(grabber.Parameters, Logger);
                            if (!string.IsNullOrEmpty(grabber.ChannelPrefix))
                                shows.ForEach(x => x.Channel = grabber.ChannelPrefix + x.Channel);
                            lst.AddRange(shows);
                        }
                }

            return lst;
        }
    }
}
