using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace livesoccertv.com
{
    public class WebSiteGrabber : GrabberBase
    {
        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();

            const string BaseUrl = "https://www.livesoccertv.com/channels/";

            var doc = XDocument.Parse(xmlParameters);
            var channels = doc.Descendants("Channels").Elements().Select(x => x.Value).ToList();
            foreach (var channel in channels)
            {
                try
                {
                    logger.WriteEntry($"Grabbing Channel {channel}", LogType.Info);
                    var url = $"{BaseUrl}{channel}/";
                    var wr = (HttpWebRequest)WebRequest.Create(url);
                    wr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0";
                    wr.Timeout = 10000;
                    var channelShows = new List<Show>();
                    using (var res = wr.GetResponse())
                    using (var sr = new StreamReader(res.GetResponseStream()))
                    {
                        var html = new HtmlAgilityPack.HtmlDocument();
                        html.LoadHtml(sr.ReadToEnd());
                        var rows = html.DocumentNode.QuerySelectorAll("tr.matchrow");
                        foreach (var row in rows)
                        {
                            try
                            {
                                var show = new Show();
                                show.Channel = channel;
                                show.Title = row.QuerySelector("td[id=match]").InnerText;
                                show.Description = row.QuerySelector("td.compcell_right").InnerText;
                                var el = row.QuerySelector("td.timecol span.ts");
                                if (el != null)
                                {
                                    show.StartTime = FromUnixTime(el.GetAttributeValue<long>("dv", 0) / 1000);
                                    channelShows.Add(show);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogException(ex);
                            }
                        }
                    }
                    FixShowsEndTimeByStartTime(channelShows);
                    channelShows.ForEach(x => { if (x.EndTime - x.StartTime > TimeSpan.FromHours(3)) x.EndTime = x.StartTime.AddHours(3); });
                    shows.AddRange(channelShows);
                }
                catch (Exception ex)
                {
                    logger.LogException(ex);
                }
            }
            return shows;
        }
    }
}