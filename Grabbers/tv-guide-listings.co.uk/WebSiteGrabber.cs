using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;
using TimeZoneConverter;
using XmlTvGenerator.Core;

namespace tv_guide_listings.co.uk
{
    public class WebSiteGrabber : GrabberBase
    {
        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();
            var doc = XDocument.Parse(xmlParameters);
            var sdElement = doc.Descendants("DaysForward").FirstOrDefault();
            var daysForward = sdElement != null && sdElement.Value != null ? Convert.ToInt32(sdElement.Value) : 0;
            var channelsElements = doc.Descendants("Channels").FirstOrDefault();
            if (channelsElements != null)
            {
                var channels = channelsElements.Elements().Select(x => x.Value).ToList();
                foreach (var channel in channels)
                {

                    for (int i = 0; i <= daysForward; i++)
                    {
                        DailyGrab(channel, DateTime.UtcNow.Date.AddDays(i), shows, logger);
                    }
                }
            }
            else
                logger.WriteEntry("No channels defined", LogType.Warning);
            
            return shows;
        }

        private void DailyGrab(string channel, DateTime d, List<Show> shows, ILogger logger)
        {
            const string Url = "https://tv-guide-listings.co.uk/";

            var fullUrl = $"{Url}{channel}/{d.DayOfWeek.ToString().ToLower()}";
            var wr = (HttpWebRequest)WebRequest.Create(fullUrl);            
            wr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:91.0) Gecko/20100101 Firefox/91.0";
            wr.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            logger.WriteEntry($"Grabbing tv-guide.listings.co.uk for date {d.ToString("yyyy-MM-dd")} channel {channel} data ...", LogType.Info);
            var res = (HttpWebResponse)wr.GetResponse();
            using (var sr = new StreamReader(res.GetResponseStream()))
            {
                var sList = new List<Show>(); 
                var html = new HtmlAgilityPack.HtmlDocument();
                html.LoadHtml(sr.ReadToEnd());
                var rows = html.QuerySelectorAll("div.row");
                foreach (var row in rows)
                {
                    var start = row.QuerySelector("div.hstart").InnerText.Trim();
                    var title = row.QuerySelector("div.title1").InnerText.Trim();
                    var description = row.QuerySelector("div.gen").InnerText.Trim();
                    var s = new Show();
                    s.Title = title;
                    s.Description = description;
                    s.StartTime = d.Date.Add(TimeSpan.Parse(start));
                    s.Channel = channel;
                    var tz = TZConvert.GetTimeZoneInfo("Europe/London");
                    var dt = DateTime.SpecifyKind(s.StartTime, DateTimeKind.Unspecified);
                    s.StartTime = TimeZoneInfo.ConvertTimeToUtc(dt, tz);
                    sList.Add(s);
                }
                FixShowsEndTimeByStartTime(sList);
                shows.AddRange(sList);
            }

        }
    }
}
