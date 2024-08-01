using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace telegraph.co.uk
{
    class WebSiteGrabber : GrabberBase
    {   
        const string urlFormat = "https://tv-listings-frontend.platforms-prod-gcp.telegraph.co.uk/platform/f55acb3b-a288-5ff7-962b-0da29811d046/region/6fc91c8e-7b21-5232-9668-495c363ea788/schedule?from={0}&to={1}";
                                  
        void DailyGrab(DateTime d, List<Show> shows, ILogger logger)
        {
            var startDate = ToUnixTime(d) * 1000;
            var toDate = ToUnixTime(d.AddDays(1)) * 1000;
            var wr = (HttpWebRequest)WebRequest.Create(string.Format(urlFormat, startDate, toDate));
#if DEBUG
            wr.Timeout = 10000;
#else
            wr.Timeout = 30000;
#endif
            wr.Method = "GET";
            wr.ContentType = "application/json";
            logger.WriteEntry(string.Format("Grabbing telegraph.co.uk for date {0} data ...", d.ToString("yyyy-MM-dd")), LogType.Info);
            wr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:91.0) Gecko/20100101 Firefox/91.0";
            wr.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            var res = (HttpWebResponse)wr.GetResponse();

            using (var sr = new StreamReader(res.GetResponseStream()))
            {
                var startDownloadTime = DateTime.Now;
                var data = new StringBuilder();
                int blockSize = 16384;
                while (!sr.EndOfStream)
                {
                    var buf = new char[blockSize];
                    var totalRead = sr.ReadBlock(buf, 0, blockSize);
                    data.Append(buf);
                    if (DateTime.Now - startDownloadTime > TimeSpan.FromSeconds(1))
                    {
                        startDownloadTime = DateTime.Now;
                        logger.WriteEntry(string.Format("Downloaded {0:#,##0} bytes so far", data.Length), LogType.Info);
                    }
                }

                var obj = JObject.Parse(new StringReader(data.ToString()).ReadToEnd());
                var channels = (JArray)obj["channels"];

                foreach (var channel in channels)
                {
                    var programs = channel["programmes"];
                    var channelName = channel["channel-title"].Value<string>();
                    foreach (var program in programs)
                    {
                        var show = new Show();
                        show.Channel = channelName;
                        show.Title = Tools.CleanupText(program["title"].Value<string>());
                        if (program["descriptionMedium"] != null)
                            show.Description = Tools.CleanupText(program["descriptionMedium"].Value<string>());
                        else
                        if (program["descriptionShort"] != null)
                            show.Description = Tools.CleanupText(program["descriptionShort"].Value<string>());
                        else
                        if (program["episodeTitle"] != null)
                            show.Description = Tools.CleanupText(program["episodeTitle"].Value<string>());
                        if (program["episodeNumber"] != null)
                            show.Episode = program["episodeNumber"].Value<int?>();
                        show.StartTime = FromUnixTime(program["start"].Value<long>() / 1000);
                        show.EndTime = FromUnixTime(program["end"].Value<long>() / 1000);
                        shows.Add(show);
                    }
                }
            }
        }

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();
            var doc = XDocument.Parse(xmlParameters);
            var sdElement = doc.Descendants("StartDate").FirstOrDefault();
            var startDateDiff = sdElement != null && sdElement.Value != null ? Convert.ToInt32(sdElement.Value) : -1;
            var edElement = doc.Descendants("EndDate").FirstOrDefault();
            var endDateDays = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;
            for (int i = startDateDiff; i <= endDateDays; i++)
            {
                DailyGrab(DateTime.UtcNow.Date.AddDays(i), shows, logger);
            }
            return shows;
        }
    }
}