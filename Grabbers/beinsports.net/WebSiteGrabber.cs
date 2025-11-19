using HtmlAgilityPack.CssSelectors.NetCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace beinsports.net
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
            logger.WriteEntry($"Grabbing bein sports", LogType.Info);

            const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:144.0) Gecko/20100101 Firefox/144.0";

            var shows = new List<Show>();

            var doc = XDocument.Parse(xmlParameters);
            var sdElement = doc.Descendants("StartDate").FirstOrDefault();
            var startDateDiff = sdElement != null && sdElement.Value != null ? Convert.ToInt32(sdElement.Value) : 0;
            var edElement = doc.Descendants("EndDate").FirstOrDefault();
            var endDateDays = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;
            var region = doc.Descendants("Region").First().Value;
            var dateFormat = "yyyy-MM-ddTHH:mm:ss.999Z";
            var startDateText = HttpUtility.UrlEncode(DateTime.UtcNow.Date.AddDays(startDateDiff).ToString(dateFormat));
            var endDateText = HttpUtility.UrlEncode(DateTime.UtcNow.Date.AddDays(endDateDays).ToString(dateFormat));

            // get channels
            var channels = new List<ChannelData>(); 
            var wr = (HttpWebRequest)WebRequest.Create("https://www.beinsports.com/api/opta/tv-channel?region=" + region);
            wr.UserAgent = UserAgent;
            using (var response = wr.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                var json = sr.ReadToEnd();
                var data = JObject.Parse(json);
                foreach (var row in data["rows"])
                {
                    channels.Add(new ChannelData { Name = row["name"].Value<string>(), Id = row["id"].Value<string>() });
                }
            }
            var channelDict = channels.ToDictionary(x => x.Id);

            var channelIds = string.Join("&",channels.Select(x => "channelIds=" + x.Id));
            var url = $"https://www.beinsports.com/api/opta/tv-event?searchKey=&startBefore={endDateText}&endAfter={startDateText}&limit=3000&" + channelIds;
            wr = (HttpWebRequest)WebRequest.Create(url);
            wr.UserAgent = UserAgent;
            using (var response = wr.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                var json = sr.ReadToEnd();
                var data = JObject.Parse(json);
                foreach (var row in data["rows"])
                {                    
                    var channelId = row["channel"]["id"].Value<string>();
                    if (channelDict.TryGetValue(channelId, out var channel))
                    {
                        var s = new Show();
                        s.Channel = channel.Name;
                        s.Title = row["title"].Value<string>();
                        s.Description = row["description"].Value<string>();
                        s.StartTime = row["startDate"].Value<DateTime>();
                        s.EndTime = row["endDate"].Value<DateTime>();
                        if (row["live"].Value<bool>())
                            s.Title = $"(Live) {s.Title}";
                        shows.Add(s);
                    }
                }
            }
            logger.WriteEntry($"bein sports found {shows.Count} shows", LogType.Info);
            return shows;
        }
    }
}
