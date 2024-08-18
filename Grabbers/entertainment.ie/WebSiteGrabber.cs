using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using TimeZoneConverter;
using XmlTvGenerator.Core;

namespace entertainment.ie
{
    public class WebSiteGrabber : GrabberBase
    {
        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            const string URL = "https://entertainment.ie/tv/{0}/?date={1}&time=all-day";

            var shows = new List<Show>();

            var doc = XDocument.Parse(xmlParameters);
            var edElement = doc.Descendants("DaysForward").FirstOrDefault();
            var daysForward = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;
            var channels = doc.Descendants("Channels").Elements().Select(x => x.Value).ToList();

            foreach (var channel in channels)
            {
                var channelShows = new List<Show>();
                for (int i = 0; i < daysForward; i++)
                {
                    var d = DateTime.Today.AddDays(i);
                    logger.WriteEntry($"Grabbing Channel {channel} date {d:yyyy-MM-dd}", LogType.Info);
                    var wr = WebRequest.Create(string.Format(URL, channel, d.ToString("dd-MM-yyyy")));
                    using (var res = wr.GetResponse())
                    using (var sr = new StreamReader(res.GetResponseStream()))
                    {
                        var html = new HtmlAgilityPack.HtmlDocument();
                        html.LoadHtml(sr.ReadToEnd());
                        var infoList = html.QuerySelector("ul.info-list");
                        var currentDate = d.Date;
                        foreach (var li in infoList.QuerySelectorAll("li.lightbox-wrapper"))
                        {
                            var s = new Show();
                            var div = li.QuerySelector("div.text-holder");                            
                            s.Title = Tools.CleanupText(div.QuerySelector("h3 a").InnerText);
                            var time = div.QuerySelector("span.time").InnerText;
                            s.StartTime = currentDate.Add(TimeSpan.Parse(time));
                            s.Channel = channel;
                            var tz = TZConvert.GetTimeZoneInfo("Europe/London");
                            var dt = DateTime.SpecifyKind(s.StartTime, DateTimeKind.Unspecified);
                            s.StartTime = TimeZoneInfo.ConvertTimeToUtc(dt, tz);
                            var infoA = div.QuerySelector("div.btn-wrap a");
                            s.Description = infoA.GetAttributeValue("data-description", string.Empty);
                            var lastShow = channelShows.LastOrDefault();
                            if (lastShow != null)
                            {
                                if (lastShow.StartTime > s.StartTime)
                                {
                                    currentDate = currentDate.AddDays(1);
                                    s.StartTime = s.StartTime.AddDays(1);
                                }
                                lastShow.EndTime = s.StartTime;
                            }
                            channelShows.Add(s);
                        }                        
                    }
                }
                shows.AddRange(channelShows);
            }                
            return shows;
        }
    }
}