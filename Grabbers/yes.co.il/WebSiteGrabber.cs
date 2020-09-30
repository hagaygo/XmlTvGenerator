using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TimeZoneConverter;
using XmlTvGenerator.Core;

namespace yes.co.il
{
    public class WebSiteGrabber : GrabberBase
    {
        class ChannelData
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public bool IncludeDescription { get; set; }
        }

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();
            var doc = XDocument.Parse(xmlParameters);
            var edElement = doc.Descendants("DaysForward").FirstOrDefault();
            var daysForward = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;

            var channelDict = new Dictionary<string, ChannelData>();

            var extraChannels = doc.Descendants("Channels").FirstOrDefault();
            if (extraChannels != null)
            {
                foreach (var chan in extraChannels.Descendants("Channel"))
                {
                    channelDict.Add(chan.Attribute("ID").Value, new ChannelData { Code = chan.Attribute("ID").Value, Name = chan.Value, IncludeDescription = chan.Attribute("Description") != null ? chan.Attribute("Description").Value == "yes" : false });
                }
            }

            var needDescriptionFetch = false;

            foreach (var c in channelDict.Keys)
            {
                var channelShows = new List<Show>();
                const string url = "https://www.yes.co.il/content/YesChannelsHandler.ashx?action=GetDailyShowsByDayAndChannelCode&dayValue={0}&channelCode={1}";
                var channel = channelDict[c];
                var currentDate = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Utc, TZConvert.GetTimeZoneInfo("Asia/Jerusalem")).Date;                
                for (int i = 0; i <= daysForward; i++)
                {
                    var d = currentDate.AddDays(i);
                    var wr = WebRequest.Create(string.Format(url, i, c));
                    wr.Timeout = 15000;
                    logger.WriteEntry(string.Format("Grabbing yes.co.il channel : {0} , days forward  :{1}", channel.Name, i), LogType.Info);
                    var res = (HttpWebResponse)wr.GetResponse();

                    var html = new HtmlAgilityPack.HtmlDocument();
                    html.Load(res.GetResponseStream(), Encoding.UTF8);

                    int dayCounter = 0;
                    foreach (var li in html.QuerySelectorAll("ul li"))
                    {
                        try
                        {
                            var s = new Show();
                            s.Channel = channel.Name;
                            var text = li.QuerySelector("span.text").InnerText;
                            var time = text.Substring(0, text.IndexOf(" "));
                            s.StartTime = d + DateTime.Parse(time).TimeOfDay;
                            if (dayCounter == 0 && s.StartTime.Hour > 18)
                                s.StartTime = s.StartTime.AddDays(-1); // first show on current day but starts on previous one
                            s.StartTime = TimeZoneInfo.ConvertTime(s.StartTime, TZConvert.GetTimeZoneInfo("Asia/Jerusalem"), TimeZoneInfo.Utc);
                            s.Description = string.Empty;
                            text = text.Substring(text.IndexOf(" - ") + 2).Trim();
                            if (channel.IncludeDescription)
                            {
                                var schedule_item_id = li.QuerySelector("a").Attributes["schedule_item_id"].Value;
                                s.Description = schedule_item_id;
                                needDescriptionFetch = true;
                            }
                            if (!string.IsNullOrEmpty(text))
                            {
                                s.Title = text;
                                channelShows.Add(s);
                                dayCounter++;
                            }                            
                        }
                        catch (Exception ex)
                        {
                            logger.WriteEntry(ex.Message + ex.StackTrace, LogType.Error);
                        }
                    }
                }                
                FixShowsEndTimeByStartTime(channelShows);                   
                shows.AddRange(channelShows);
            }

            if (needDescriptionFetch)
            {
                var lst = shows.Where(x => x.Description.Length > 0).ToList();
                Parallel.ForEach(lst, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (s) =>
                {
                    var schedule_item_id = s.Description;
                    var wr2 = WebRequest.Create(string.Format("https://www.yes.co.il/content/YesChannelsHandler.ashx?action=GetProgramDataByScheduleItemID&ScheduleItemID={0}", schedule_item_id));
                    wr2.Timeout = 15000;
                    logger.WriteEntry(string.Format("Grabbing yes.co.il channel : {0} item description  :{1}", s.Channel, schedule_item_id), LogType.Info);
                    var res2 = (HttpWebResponse)wr2.GetResponse();
                    using (var sr = new StreamReader(res2.GetResponseStream()))
                    {
                        var resultText = sr.ReadToEnd();
                        if (resultText.Length > 2)
                        {
                            var obj = Newtonsoft.Json.Linq.JObject.Parse(resultText);
                            s.Description = obj["PreviewText"].ToString();
                        }
                    }
                });
            }

            return CleanupSameTimeStartEndShows(shows);            
        }
    }
}
