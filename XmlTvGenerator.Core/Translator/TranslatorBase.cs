using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XmlTvGenerator.Core.Translator.Cache;

namespace XmlTvGenerator.Core.Translator
{
    public abstract class TranslatorBase
    {
        protected CacheManagerBase Cache { get; private set; }

        int _maxDegreeOfParallelism = 1;

        public TranslatorBase(CacheManagerBase cache, int maxDegreeOfParallelism)
        {
            Cache = cache;
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
        }


        public abstract string Translate(Language from, Language to, string text);

        public void TranslateShows(List<Show> shows, Language sourceLang, Language targetLang, ILogger logger)
        {
            Debug.WriteLine("Found {0} Shows", shows.Count);
            Debug.WriteLine("Started Translating...");
            int counter = 0;
            
            var lockObject = new object();
            var lastTime = DateTime.Now;
            try
            {
                Parallel.ForEach(shows, new ParallelOptions() { MaxDegreeOfParallelism = _maxDegreeOfParallelism }, show =>
                {
                    show.Title = Tools.CleanupText(Translate(sourceLang, targetLang, show.Title));
                    if (!string.IsNullOrEmpty(show.Description))
                        show.Description = Tools.CleanupText(Translate(sourceLang, targetLang, show.Description));
                    lock (lockObject)
                    {
                        counter++;
                    }
                    if (DateTime.Now - lastTime > TimeSpan.FromSeconds(10))
                    {
                        lastTime = DateTime.Now;
                        var text = string.Format("Translator Finished {0}/{1} {2:0.00}%", counter, shows.Count, counter * 100.0 / shows.Count);
                        Debug.WriteLine(text);
                        lock (lockObject)
                            logger.WriteEntry(text, LogType.Info);
                    }
                }
                );
            }
            catch
            {
                Cache.SaveCache();
                throw;
            }
            Cache.SaveCache();
            Debug.WriteLine("Finished Translating.");
        }
    }
}
