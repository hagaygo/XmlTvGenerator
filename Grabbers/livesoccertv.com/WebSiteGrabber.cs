using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace livesoccertv.com
{
    public class WebSiteGrabber : GrabberBase
    {
        string[] userAgents = { 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:138.0) Gecko/20100101 Firefox/141.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/142.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:143.0) Gecko/20100101 Firefox/143.0"
        };

        int _userAgentCounter = 0;

        string GetNextUserAgent()
        {
            if (_userAgentCounter >= userAgents.Length)
            {
                _userAgentCounter = 0;
            }
            return userAgents[_userAgentCounter++];
        }

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            const string ErrorPrefix = "livesoccertv.com";

            logger.WriteEntry("Started livesoccertv.com grab", LogType.Info);

            var shows = new List<Show>();

            const string BaseUrl = "https://www.livesoccertv.com/channels/";

            var doc = XDocument.Parse(xmlParameters);
            var channels = doc.Descendants("Channels").Elements().Select(x => x.Value).ToList();
            foreach (var channel in channels)
            {
                try
                {
                    const int WaitMiliSeconds = 2000;

                    logger.WriteEntry($"Grabbing Channel {channel} with wait of {WaitMiliSeconds} Miliseconds", LogType.Info);
                    Task.Delay(WaitMiliSeconds).Wait();
                    var url = $"{BaseUrl}{channel}/";
                    var wr = (HttpWebRequest)WebRequest.Create(url);
                    wr.UserAgent = GetNextUserAgent();
                    wr.Timeout = 6000;
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
                                show.Title = row.QuerySelector("td[id=match]").InnerText?.Trim();
                                show.Description = row.QuerySelector("td.compcell_right").InnerText?.Trim();
                                var el = row.QuerySelector("td.timecol span.ts");
                                if (el != null)
                                {
                                    show.StartTime = FromUnixTime(el.GetAttributeValue<long>("dv", 0) / 1000);
                                    channelShows.Add(show);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogException(ex, ErrorPrefix);
                            }
                        }
                    }
                    FixShowsEndTimeByStartTime(channelShows);
                    channelShows.ForEach(x => { if (x.EndTime - x.StartTime > TimeSpan.FromHours(3)) x.EndTime = x.StartTime.AddHours(3); });
                    shows.AddRange(channelShows);
                }
                catch (Exception ex)
                {
                    logger.LogException(ex, ErrorPrefix);
                }
            }
            logger.WriteEntry("Finished livesoccertv.com grab", LogType.Info);
            return shows;
        }
    }
}