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
    public class WebSiteGrabber : GrabberBase
    {
        const string urlFormat = "https://api.starhubtvplus.com/epg?operationName=webFilteredEpg&variables={{\"dateFrom\":\"{0}\",\"dateTo\":\"{1}\"}}&query=query webFilteredEpg($category: String, $dateFrom: DateWithoutTime, $dateTo: DateWithoutTime!) {{\r\n  nagraEpg(category: $category) {{\r\n    items {{\r\n      channelId: tvChannel\r\n      id\r\n      image\r\n      isIptvMulticast\r\n      isOttUnicast\r\n      title: description\r\n      programs: programsByDate(dateFrom: $dateFrom, dateTo: $dateTo) {{\r\n        channel {{\r\n          isIptvMulticast\r\n          isOttUnicast\r\n          __typename\r\n        }}\r\n        endTime\r\n        id\r\n        startOverSupport\r\n        startTime\r\n        title\r\n        __typename\r\n      }}\r\n      __typename\r\n    }}\r\n    __typename\r\n  }}\r\n}}\r\n";
        const string DateFormat = "yyyy-MM-dd";

        List<Show> Grab(GrabParametersBase p, ILogger logger)
        {
            var pp = (GrabParameters)p;
            var shows = new List<Show>();
            var wr = WebRequest.Create(String.Format(urlFormat, pp.Date.Date.ToString(DateFormat), pp.Date.Date.AddDays(1).ToString(DateFormat)));
            wr.Method = "GET";
            wr.ContentType = "application/json";
            wr.Headers.Add("x-application-key", "5ee2ef931de1c4001b2e7fa3_5ee2ec25a0e845001c1783dc");
            wr.Headers.Add("x-application-session", "0SSK4001B2E7FA34001B2E7F8E1890DC266E");
            logger.WriteEntry(string.Format("Grabbing starhubtvplus.com date {0} ...", pp.Date.ToString(DateFormat)), LogType.Info);
            var res = (HttpWebResponse)wr.GetResponse();

            using (var sr = new StreamReader(res.GetResponseStream()))
            {
                var startDownloadTime = DateTime.Now;
                var data = new StringBuilder();
                int blockSize = 16384;
                while (!sr.EndOfStream)
                {
                    var buf = new char[blockSize];
                    var totalRead = sr.ReadBlock(buf, 0, blockSize);
                    data.Append(buf);
                    if (DateTime.Now - startDownloadTime > TimeSpan.FromSeconds(1))
                    {
                        startDownloadTime = DateTime.Now;
                        logger.WriteEntry(string.Format("Downloaded {0:#,##0} bytes so far", data.Length), LogType.Info);
                    }
                }

                var obj = JObject.Parse(new StringReader(data.ToString()).ReadToEnd());
                var channels = obj["data"]["nagraEpg"]["items"];

                foreach (var channel in channels)
                {
                    var programs = channel["programs"];
                    var channelName = channel.Value<string>("channelId");
                    foreach (var program in programs)
                    {
                        var show = new Show();
                        show.Channel = channelName;
                        show.Title = Tools.CleanupText(program["title"].ToString());
                        show.StartTime = FromUnixTime(program.Value<long>("startTime") / 1000);
                        show.EndTime = FromUnixTime(program.Value<long>("endTime") / 1000);
                        shows.Add(show);
                    }
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

            //_channelsDict = GetChannelsDict(logger);

            var maxTries = 15;
            var retryCount = 0;

            for (int i = startDateDiff; i < endDateDays; i++)
            {
                try
                {
                    var p = new GrabParameters() { Date = DateTime.Now.Date.AddDays(i) };
                    shows.AddRange(Grab(p, logger));

                }
                catch (Exception ex)
                {
                    if (retryCount < maxTries)
                    {
                        retryCount++;
                        logger.WriteEntry($"{ex.Message.ToLower()} , retry #{retryCount}", LogType.Warning);
                        i--;
                        continue;
                    }
                    else
                        throw;
                }
            }
            return shows;
        }
    }

    public class GrabParameters : GrabParametersBase
    {
        public DateTime Date { get; set; }
    }
}
