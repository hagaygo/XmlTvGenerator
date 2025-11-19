using HtmlAgilityPack.CssSelectors.NetCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace hot.net.il
{
    public class ChannelData
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class WebSiteGrabber : GrabberBase
    {
        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var doc = XDocument.Parse(xmlParameters);
            var startDayDiffElement = doc.Descendants("StartDate").FirstOrDefault();
            var endDayDiffElement = doc.Descendants("EndDate").FirstOrDefault();
            var channels = new List<ChannelData>();            
            foreach (var c in doc.Descendants("Channels")?.Descendants())
            {
                var chan = new ChannelData();
                chan.Id = c.Attribute("Id").Value;
                chan.Name = c.Attribute("Name").Value;
                channels.Add(chan);
            }
            var channelDict = channels.ToDictionary(x => x.Id);

            var startDayDiff = startDayDiffElement != null ? Convert.ToInt32(startDayDiffElement.Value) : 0;
            var endDayDiff = endDayDiffElement != null ? Convert.ToInt32(endDayDiffElement.Value) : 3;
            const string DateFormat = "yyyy/MM/dd 00:00:00";
            var startDateText = DateTime.Now.AddDays(startDayDiff).ToString(DateFormat);
            var endDateText = DateTime.Now.AddDays(endDayDiff).ToString(DateFormat);

            var wr = (HttpWebRequest)WebRequest.Create("https://www.hot.net.il/HotCmsApiFront/api/ProgramsSchedual/GetProgramsSchedual");
            wr.UserAgent = "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:144.0) Gecko/20100101 Firefox/144.0";
            wr.Accept = "application/json";
            wr.Referer = "https://www.hot.net.il/heb/tv/tvguide/";
            wr.ContentType = "application/json";
            wr.Method = "POST";
            wr.Headers.Add("Origin", "https://www.hot.net.il");
            using (var sw = new StreamWriter(wr.GetRequestStream()))
                sw.Write("{ \"ProgramsStartDateTime\": \"" + startDateText + "\", \"ProgramsEndDateTime\": \"" + endDateText + " \" }");
            logger.WriteEntry($"Grabbing Hot.net.il", LogType.Info);
            using (var res = (HttpWebResponse)wr.GetResponse())
            using (var sr = new StreamReader(res.GetResponseStream()))
            {
                var json = sr.ReadToEnd();
                json = JsonConvert.DeserializeObject<string>(json);
                var data = JObject.Parse(json)["data"]["programsDetails"];
                var shows = new List<Show>();

                foreach (var item in data)
                {
                    var channelId = item["channelID"].Value<string>();                    
                    if (channelDict.TryGetValue(channelId, out var channelData))
                    {
                        var show = new Show();
                        show.Channel = channelData.Name;
                        show.Title = item["programTitle"].Value<string>();
                        show.Description = item["synopsis"].Value<string>();
                        show.StartTime = DateTime.SpecifyKind(item["programStartTime"].Value<DateTime>(), DateTimeKind.Local);
                        show.EndTime = DateTime.SpecifyKind(item["programEndTime"].Value<DateTime>(), DateTimeKind.Local);
                        shows.Add(show);
                    }
                }

                logger.WriteEntry($"Hot.net.il found {shows.Count} shows", LogType.Info);
                return shows;
            }
        }
    }
}
