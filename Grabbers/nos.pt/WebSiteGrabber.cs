using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TimeZoneConverter;
using XmlTvGenerator.Core;

namespace nos.pt
{
    public class WebSiteGrabber : GrabberBase
    {
        class ChannelData
        {
            public string Code { get; set; }
            public string Name { get; set; }            
        }        

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var lst = new List<Show>();

            var url = "https://www.nos.pt/particulares/televisao/guia-tv/Pages/channel.aspx?channel={0}";

            var doc = XDocument.Parse(xmlParameters);
            var channelDict = new Dictionary<string, ChannelData>();

            var extraChannels = doc.Descendants("Channels").FirstOrDefault();
            if (extraChannels != null)
            {
                foreach (var chan in extraChannels.Descendants("Channel"))
                {
                    channelDict.Add(chan.Attribute("ID").Value, new ChannelData { Code = chan.Attribute("ID").Value, Name = chan.Value });
                }
            }

            foreach (var channelId in channelDict.Keys)
            {
                var wr = (HttpWebRequest)WebRequest.Create(string.Format(url, channelId));
                var res = (HttpWebResponse)wr.GetResponse();

                logger.WriteEntry($"Grabbing nos.pt channel {channelDict[channelId].Name}", LogType.Info);

                var html = new HtmlAgilityPack.HtmlDocument();
                using (var response = res.GetResponseStream())
                {
                    html.Load(response);
                    var programs = html.DocumentNode.QuerySelector("div#programs-container");
                    var days = programs.QuerySelectorAll("div.programs-day-list");
                    var firstDay = true;
                    var dayDelta = 0;
                    foreach (var day in days)
                    {
                        var id = day.Attributes["id"].Value.Substring(3);
                        if (firstDay)
                        {
                            var d = Convert.ToInt32(id);
                            if (DateTime.Now.Date.Day != d)                                
                                dayDelta = -1;
                            firstDay = false;
                        }                        
                        var items = day.QuerySelectorAll("li");
                        foreach (var item in items)
                        {
                            var program = Tools.CleanupText(item.QuerySelector("span.program").InnerText);
                            var duration = Tools.CleanupText(item.QuerySelector("span.duration").InnerText);

                            var durationTokens = duration.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                            var start = DateTime.Parse(durationTokens[0]).AddDays(dayDelta);
                            var end = DateTime.Parse(durationTokens[1]).AddDays(dayDelta);                            
                            var s = new Show();
                            s.Channel = channelDict[channelId].Name;
                            s.Title = program;
                            s.StartTime = start;
                            s.StartTime = TimeZoneInfo.ConvertTime(s.StartTime, TZConvert.GetTimeZoneInfo("Europe/Lisbon"), TimeZoneInfo.Utc);
                            s.EndTime = end;
                            s.EndTime = TimeZoneInfo.ConvertTime(s.EndTime, TZConvert.GetTimeZoneInfo("Europe/Lisbon"), TimeZoneInfo.Utc);
                            if (s.StartTime > s.EndTime)
                                s.StartTime = s.StartTime.AddDays(-1);
                            lst.Add(s);
                        }
                        dayDelta++;
                    }                    
                }
            }

            return lst;
        }
    }
}
