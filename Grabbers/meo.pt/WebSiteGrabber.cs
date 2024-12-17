using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace meo.pt
{
    internal class WebSiteGrabber : GrabberBase
    {
        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            const string Url = "https://authservice.apps.meo.pt/Services/GridTv/GridTvMng.svc/getProgramsFromChannels";

            var shows = new List<Show>();

            var doc = XDocument.Parse(xmlParameters);
            var edElement = doc.Descendants("DaysForward").FirstOrDefault();
            var daysForward = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;
            var channels = doc.Descendants("Channels").Elements().Select(x => x.Value).ToList();

            var handler = new HttpClientHandler()
            {

                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            using (var http = new HttpClient(handler))
            {
                http.DefaultRequestHeaders.Add("Origin", "https://www.meo.pt");
                http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:120.0) Gecko/20100101 Firefox/120.0");
                http.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                for (int i = 0; i <= daysForward; i++)
                {
                    var date = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(i), DateTimeKind.Unspecified);
                    date = TimeZoneInfo.ConvertTime(date, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"), TimeZoneInfo.Utc);
                    var postData = new
                    {
                        service = "channelsguide",
                        channels,
                        dateStart = date,
                        dateEnd = date.AddDays(1),
                        accountID = "",
                    };
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(postData);
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    logger.WriteEntry($"Grabbing meo.pt date {date:yyyy-MM-dd}", LogType.Info);
                    var res = http.PostAsync(Url, content).Result;
                    var data = JObject.Parse(res.Content.ReadAsStringAsync().Result);
                    foreach (var chan in data["d"]["channels"])
                    {
                        var channelName = chan.Value<string>("name");
                        foreach (var program in chan["programs"])
                        {
                            var s = new Show();
                            s.Channel = channelName;
                            s.Title = program.Value<string>("name");
                            s.StartTime = (date + TimeSpan.Parse(program.Value<string>("timeIni")));
                            s.EndTime = (date + TimeSpan.Parse(program.Value<string>("timeEnd")));

                            if (s.StartTime > s.EndTime)
                                s.StartTime = s.StartTime.AddDays(-1);
                            shows.Add(s);
                        }
                    }
                }
            }

            return shows;
        }
    }
}