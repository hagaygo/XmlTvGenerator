using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XmlTvGenerator.Core;
using XmlTvGenerator.Core.Translator;
using XmlTvGenerator.Core.Translator.Cache;

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
                List<Show> shows = GetShows(Logger);
                if (shows.Count == 0)
                    Console.WriteLine("No shows found.");
                else
                {
                    var xmltv = new XmlTv();
                    if (File.Exists(config.OutputPath))
                        File.Delete(config.OutputPath);
                    using (var f = new FileStream(config.OutputPath, FileMode.CreateNew))
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

        private static List<Show> GetShows(ILogger logger)
        {            
            var config = Config.GetInstance();
            CacheManagerBase fileCache;
            if (config.TranslatorCacheSettings != null)
                fileCache = new FileCacheManager(config.TranslatorCacheSettings.OutputFile);
            else
                fileCache = new DummyCacheManager();
            var lst = new List<Show>();
            if (config.Grabbers.Count == 0)
                Console.WriteLine("No grabbers defined in config");
            else
                foreach (var grabber in config.Grabbers)
                {
                    try
                    {
                        var a = Assembly.LoadFrom(grabber.Path);                        
                        foreach (var t in a.GetTypes())
                            if (t.IsSubclassOf(typeof(GrabberBase)))
                            {
                                var gb = (GrabberBase)Activator.CreateInstance(t);
                                var shows = gb.Grab(grabber.Parameters, Logger);
                                if (!string.IsNullOrEmpty(grabber.ChannelPrefix))
                                    shows.ForEach(x => x.Channel = grabber.ChannelPrefix + x.Channel);
                                if (grabber.Translation != null)
                                {
                                    Logger.WriteEntry("Starting translation of " + t.FullName, LogType.Info);
                                    var g = new GoogleTranslator(fileCache);
                                    g.TranslateShows(shows, grabber.Translation.From, grabber.Translation.To, logger);
                                    Logger.WriteEntry("Finished translation of " + t.FullName, LogType.Info);
                                }
                                lst.AddRange(shows);
                            }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteEntry("error on grabber " + grabber.Path + " " + ex.Message, LogType.Error);
                        if (config.HaltOnGrabberError)
                            throw ex;
                    }
                }

            return lst;
        }
    }
}
