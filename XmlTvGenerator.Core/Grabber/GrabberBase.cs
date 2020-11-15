using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XmlTvGenerator.Core
{
    public abstract class GrabberBase : IDisposable
    {
        public GrabberBase()
        {
            Init();
        }
        

        public DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public long ToUnixTime(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }

        protected virtual bool UseGenericDataDictionary => false;

        protected Dictionary<string, string> GenericDictionary { get; private set; }

        private void LoadDictionary(Dictionary<string, string> d, string filename)
        {
            using (var sr = new StreamReader(filename))
            {
                while (!sr.EndOfStream)
                {
                    var key = Base64Decode(sr.ReadLine());
                    var value = Base64Decode(sr.ReadLine());
                    d[key] = value;
                }
            }
        }

        string GenericDictionaryFilename
        {
            get { return GetType().FullName + "Dict.txt"; }
        }

        public void Init()
        {
            if (UseGenericDataDictionary)
            {
                GenericDictionary = new Dictionary<string, string>();
                if (File.Exists(GenericDictionaryFilename))
                {                    
                    LoadDictionary(GenericDictionary, GenericDictionaryFilename);
                }
            }
        }

        public void DeInit()
        {
            if (UseGenericDataDictionary && GenericDictionary != null)
            {
                SaveDictiobary(GenericDictionary, GenericDictionaryFilename);
                GenericDictionary = null;
            }
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        private void SaveDictiobary(Dictionary<string, string> d, string filename)
        {
            File.Delete(filename);
            using (var sw = new StreamWriter(filename))
            {
                foreach (var key in d.Keys)
                {
                    sw.WriteLine(Base64Encode(key));
                    sw.WriteLine(Base64Encode(d[key]));
                }
            }
        }

        public abstract List<Show> Grab(string xmlParameters, ILogger logger);

        protected List<Show> CleanupSameTimeStartEndShows(List<Show> shows)
        {
            return shows.Where(x => x.StartTime != x.EndTime).ToList();
        }

        protected void FixShowsEndTimeByStartTime(List<Show> shows)
        {
            if (shows.Count > 1)
            {
                for (int i = shows.Count - 2; i >= 0; i--)
                {
                    shows[i].EndTime = shows[i + 1].StartTime;
                }
                var s = shows[shows.Count - 1];
                if (s.StartTime > s.EndTime)
                    s.EndTime = s.StartTime;
            }
            if (shows.Count >= 1)
                if (shows[shows.Count -1].EndTime == shows[shows.Count - 1].StartTime)
                    shows[shows.Count -1].EndTime = shows[shows.Count-1].StartTime.AddHours(2);
        }

        public void Dispose()
        {
            DeInit();
        }
    }
}
