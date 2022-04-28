using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml.Linq;
using TimeZoneConverter;
using XmlTvGenerator.Core;

namespace ontvtonight.com
{
    public class WebSiteGrabber : GrabberBase
    {
        const string URL = "https://www.ontvtonight.com/guide/listings/channel/{0}?dt={1}";

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();
            var channelDict = new Dictionary<string,string>();

            var doc = XDocument.Parse(xmlParameters);
            var edElement = doc.Descendants("DaysForward").FirstOrDefault();
            var daysForward = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;

            var channelsElement = doc.Descendants("Channels").FirstOrDefault();
            if (channelsElement != null)
            {
                foreach (var c in channelsElement.Elements().Where(x => x.Name == "Channel"))
                    channelDict.Add(c.Value, c.Attribute("url").Value);                
            }
            else
            {
                logger.WriteEntry("No channels defined", LogType.Warning);
                return shows;
            }

            var currentNow = TimeZoneInfo.ConvertTime(DateTime.Now.ToUniversalTime(), TimeZoneInfo.Utc, TZConvert.GetTimeZoneInfo("America/New_York"));

            foreach (var channel in channelDict.Keys)
            {
                var channelShows = new List<Show>();
                try
                {
                    for (int i = 0; i < daysForward; i++)
                    {
                        var currentDate = currentNow.Date.AddDays(i);
                        var dt = currentDate.ToString("yyyy-MM-dd");
                        var wr = (HttpWebRequest)WebRequest.Create(string.Format(URL, channelDict[channel], dt));
                        wr.AllowAutoRedirect = false;
                        logger.WriteEntry(string.Format("Grabbing ontvtonight channel : {0} , date : {1} ...", channel, dt), LogType.Info);
                        var res = (HttpWebResponse)wr.GetResponse();
                        var html = new HtmlAgilityPack.HtmlDocument();
                        using (var data = res.GetResponseStream())
                        {
                            html.Load(data);
                            var rows = html.QuerySelectorAll("table.table-hover tr");
                            foreach (var row in rows)
                            {
                                var tds = row.QuerySelectorAll("td");
                                if (tds.Any())
                                {
                                    var time = tds.First().QuerySelector("h5").InnerText.Trim();
                                    var desc = tds.Skip(1).First();
                                    var title = desc.QuerySelector("a").InnerText.Trim();
                                    var subTitle = desc.QuerySelector("h6")?.InnerText?.Trim();
                                    if (subTitle != null && subTitle.Contains("\n"))
                                        subTitle = CleanLines(subTitle);
                                    var show = new Show();
                                    show.Channel = channel.ToString();
                                    show.Title = HttpUtility.HtmlDecode(title);
                                    show.Description = HttpUtility.HtmlDecode(subTitle);
                                    var startTime =  currentDate.Add(DateTime.Parse(time).TimeOfDay);
                                    show.StartTime = TimeZoneInfo.ConvertTime(startTime, TZConvert.GetTimeZoneInfo("America/New_York"), TimeZoneInfo.Utc);
                                    channelShows.Add(show);
                                }
                            }                            
                        }
                    }
                    FixShowsEndTimeByStartTime(channelShows);
                    shows.AddRange(channelShows);
                }
                catch (Exception ex)
                {
                    logger.LogException(ex);
                }
            }

            var lst = new List<Show>();
            foreach (var chan in shows.Select(x => x.Channel).Distinct())
            {
                var channelShows = shows.Where(x => x.Channel == chan).OrderBy(x => x.StartTime).ToList();
                FixShowsEndTimeByStartTime(channelShows);
                lst.AddRange(channelShows);
            }
            return lst;
        }

        private string CleanLines(string subTitle)
        {
            return string.Join("\n", subTitle.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 1));
        }

        private DateTime GetDate(string startTime)
        {
            var tokens = startTime.Split(' ');
            var date = DateTime.Parse(tokens[0]);
            var time = DateTime.Parse(tokens[1]).TimeOfDay;
            var tz = TZConvert.GetTimeZoneInfo("Pacific Standard Time");
            var dt = DateTime.SpecifyKind(date + time, DateTimeKind.Unspecified);
            dt = TimeZoneInfo.ConvertTimeToUtc(dt, tz);
            return dt;
        }
    }
}
