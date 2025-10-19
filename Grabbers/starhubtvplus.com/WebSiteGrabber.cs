using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace starhubtvplus.com
{
    public class ChannelData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int Number { get; set; }
    }

    public class WebSiteGrabber : GrabberBase
    {        
        const string DateFormat = "yyyy-MM-dd";

        string GetJson(string url)
        {
            var wr = WebRequest.Create(url);
            using (var res = (HttpWebResponse)wr.GetResponse())
            {
                using (var sr = new StreamReader(res.GetResponseStream()))
                    return sr.ReadToEnd();
            }
        }

        List<Show> Grab(GrabParametersBase p, ILogger logger)
        {
            var pp = (GrabParameters)p;
            var startTime = ToUnixTime(pp.Date);
            var endTime = ToUnixTime(pp.Date.AddDays(1));

            logger.WriteEntry($"Starthub Grabbing date {pp.Date.ToString(DateFormat)}...", LogType.Info);

            var epgJson = GetJson($"https://waf-starhub-metadata-api-p001.ifs.vubiquity.com/v3.1/epg/schedules?locale=en_US&locale_default=en_US&device=1&in_channel_id={pp.ChannelGuids}&gt_end={startTime}&lt_start={endTime}&limit=500&page=1");
            var epgObj = JObject.Parse(epgJson);

            var shows = new List<Show>();

            foreach (var r in epgObj["resources"])
            {
                if (r.Value<string>("metatype") == "Schedule")
                {
                    var s = new Show();
                    var channelId = r.Value<string>("channel_id");
                    s.Channel = pp.ChannelDict[channelId].Title;
                    s.Title = r.Value<string>("title");
                    s.Description = r.Value<string>("description");
                    var serie_title = r.Value<string>("serie_title");
                    if (!string.IsNullOrEmpty(serie_title))
                    {
                        s.Description = $"{s.Title}\n{s.Description}";
                        s.Title = serie_title;
                    }
                    s.StartTime = FromUnixTime(r.Value<int>("start"));
                    s.EndTime = FromUnixTime(r.Value<int>("end"));
                    shows.Add(s);
                }
            }

            return shows;
        }

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();
            var doc = XDocument.Parse(xmlParameters);
            var sdElement = doc.Descendants("StartDate").FirstOrDefault();
            var startDateDiff = sdElement != null && sdElement.Value != null ? Convert.ToInt32(sdElement.Value) : -1;
            var edElement = doc.Descendants("EndDate").FirstOrDefault();
            var endDateDays = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;
            var selectedChannels = doc.Descendants("Channels").Elements().Select(x => x.Value).ToList();

            logger.WriteEntry("Getting Starthub channels data...", LogType.Info);
            var channelsJson = GetJson("https://waf-starhub-metadata-api-p001.ifs.vubiquity.com/v3.1/epg/channels?locale=en_US&locale_default=en_US&device=1&limit=250&page=1");
            var channelsList = new List<ChannelData>();
            var channelsObj = JObject.Parse(channelsJson);
            foreach (var r in channelsObj["resources"])
            {
                if (r.Value<string>("metatype") == "Channel")
                {
                    channelsList.Add(new ChannelData
                    {
                        Id = r.Value<string>("id"),
                        Title = r.Value<string>("title"),
                        Number = r.Value<int>("number")
                    });
                }
            }


            var grabChannelsIds = new List<string>();
            foreach (var c in selectedChannels)
            {
                var chan = channelsList.FirstOrDefault(y => y.Title.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0);
                if (chan != null)
                {
                    grabChannelsIds.Add(chan.Id);
                    if (chan.Title != c)
                        chan.Title = c;
                }
                else
                    logger.WriteEntry($"Starthub channel epg not found for {c}", LogType.Error);
            }                

            logger.WriteEntry($"Finished channel data (found {grabChannelsIds.Count} channels)", LogType.Info);

            var channelGuids = string.Join(",", grabChannelsIds);

            for (int i = startDateDiff; i < endDateDays; i++)
            {
                var p = new GrabParameters() { Date = DateTime.Now.Date.AddDays(i), ChannelGuids = channelGuids, ChannelDict = channelsList.ToDictionary(x => x.Id) };
                shows.AddRange(Grab(p, logger));
            }
            return shows;
        }
    }

    public class GrabParameters : GrabParametersBase
    {
        public DateTime Date { get; set; }
        public string ChannelGuids { get; set; }
        public Dictionary<string, ChannelData> ChannelDict { get; internal set; }
    }
}