using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using TimeZoneConverter;
using XmlTvGenerator.Core;

namespace yes.co.il
{
    public class WebSiteGrabber : GrabberBase
    {
        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();
            var doc = XDocument.Parse(xmlParameters);
            var edElement = doc.Descendants("DaysForward").FirstOrDefault();
            var daysForward = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;

            var channelDict = new Dictionary<string, string>();

            var extraChannels = doc.Descendants("Channels").FirstOrDefault();
            if (extraChannels != null)
            {
                foreach (var chan in extraChannels.Descendants("Channel"))
                {
                    channelDict.Add(chan.Attribute("ID").Value, chan.Value);
                }
            }

            foreach (var c in channelDict.Keys)
            {
                const string url = "https://www.yes.co.il/content/YesChannelsHandler.ashx?action=GetDailyShowsByDayAndChannelCode&dayValue={0}&channelCode={1}";
                var channelName = channelDict[c];
                var currentDate = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Utc, TZConvert.GetTimeZoneInfo("Asia/Jerusalem")).Date;
                for (int i = 0; i <= daysForward; i++)
                {
                    var d = currentDate.AddDays(i);
                    var wr = WebRequest.Create(string.Format(url, i, c));
                    wr.Timeout = 15000;
                    logger.WriteEntry(string.Format("Grabbing yes.co.il channel : {0} , days forward  :{1}", channelName, i), LogType.Info);
                    var res = (HttpWebResponse)wr.GetResponse();

                    var html = new HtmlAgilityPack.HtmlDocument();
                    html.Load(res.GetResponseStream(), Encoding.UTF8);

                    foreach (var li in html.QuerySelectorAll("ul li"))
                    {
                        var s = new Show();
                        s.Channel = channelName;
                        var text = li.QuerySelector("span.text").InnerText;
                        var time = text.Substring(0, text.IndexOf(" "));
                        s.StartTime = d + DateTime.Parse(time).TimeOfDay;
                        s.StartTime = TimeZoneInfo.ConvertTime(s.StartTime, TZConvert.GetTimeZoneInfo("Asia/Jerusalem"), TimeZoneInfo.Utc);
                        text = text.Substring(text.IndexOf(" - ") + 2).Trim();
                        s.Title = text;
                        shows.Add(s);
                    }
                }
            }
            FixShowsEndTimeByStartTime(shows);
            return shows;
        }
    }
}
