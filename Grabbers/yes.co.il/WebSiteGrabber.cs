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
                    channelDict.Add(chan.Attribute("ID").Value, new ChannelData { Code = chan.Attribute("ID").Value, Name = chan.Value });
                }
            }
            
            //var channelsJSON = GetChannelsJSON();
            //var channelList = Newtonsoft.Json.Linq.JObject.Parse(channelsJSON)["list"];

            for (int i = 0; i <= daysForward; i++)
            {
                var d = DateTime.UtcNow.AddDays(i);
                logger.WriteEntry($"Grabbing yes.co.il date {d:yyyy-MM-dd} ...", LogType.Info);
                Newtonsoft.Json.Linq.JToken scheduleList;
                try
                {
                    var scheduleJSON = GetScheduleJSON(d, logger);
                    scheduleList = Newtonsoft.Json.Linq.JObject.Parse(scheduleJSON)["list"];
                }
                catch (Exception ex)
                {
                    logger.WriteEntry($"Error parsing JSON data , {ex.Message} , retrying...", LogType.Error);
                    var scheduleJSON = GetScheduleJSON(d, logger);
                    scheduleList = Newtonsoft.Json.Linq.JObject.Parse(scheduleJSON)["list"];
                }

                foreach (var si in scheduleList)
                {
                    var channelId = si.Value<string>("channelID");
                    if (channelDict.TryGetValue(channelId, out var chan))
                    {
                        var s = new Show();
                        try
                        {                            
                            s.Channel = chan.Name;
                            s.Title = si.Value<string>("scheduleItemName");                            
                            s.Description = si.Value<string>("scheduleItemSynopsis");
                            s.StartTime = si.Value<DateTime>("startDate");
                            s.StartTime = DateTime.SpecifyKind(s.StartTime, DateTimeKind.Local).Date;
                            var startTime = si.Value<DateTime>("startTime").TimeOfDay;
                            s.StartTime = s.StartTime.Add(startTime);                            
                            s.StartTime = TimeZoneInfo.ConvertTime(s.StartTime, TimeZoneInfo.Utc);
                            s.EndTime = s.StartTime.Add(si.Value<DateTime>("broadcastItemDuration").TimeOfDay);
                            SetupDescription(s);
                            shows.Add(s);
                        }
                        catch (Exception ex)
                        {
                            logger.WriteEntry($"Error on show  {s.Title} , channel {s.Channel} , {ex.Message}", LogType.Error);
                        }
                    }
                }
            }

            return CleanupSameTimeStartEndShows(shows);
        }

        private string GetJSON(string path, string extraParameters, ILogger logger)
        {
            var host = "www.yes.co.il";
            var ckc = new CookieContainer();
            var url = $"https://{host}/content/tvguide";
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.CookieContainer = ckc;
            var res = (HttpWebResponse)req.GetResponse();
            string p_auth = null;
            using (var sr = new StreamReader(res.GetResponseStream()))
            {
                const string textToSearch = ";Liferay.authToken=";
                var mainPageHtml = sr.ReadToEnd();
                var idx = mainPageHtml.IndexOf(textToSearch);
                var val = mainPageHtml.Substring(idx + textToSearch.Length + 1, 30);
                val = val.Split(';')[0];
                p_auth = val.Substring(0, val.Length - 1);
            }
            url = $"https://{host}/o/yes/servletlinearsched/{path}";
            req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            var postData = "p_auth=" + p_auth;
            if (!string.IsNullOrEmpty(extraParameters))
                postData += "&" + extraParameters;
            var data = System.Text.Encoding.ASCII.GetBytes(postData);
            req.ContentType = "application/x-www-form-urlencoded";
            req.Headers.Add("Pragma", "no-cache");
            req.ContentLength = data.Length;
            using (var stream = req.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) Gecko/20100101 Firefox/86.0";
            req.CookieContainer = ckc;            
            req.CookieContainer.Add(new Cookie("LFR_SESSION_STATE_33706", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), "/", $"{host}"));
            
            req.Headers.Add("X-Requested-With", "XMLHttpRequest");
            req.Accept = "text/plain, */*; q=0.01";
            res = (HttpWebResponse)req.GetResponse();
            using (var sr = new StreamReader(res.GetResponseStream()))
            {
                var startDownloadTime = DateTime.Now;
                var sb = new StringBuilder();
                int blockSize = 65536;
                while (!sr.EndOfStream)
                {
                    var buf = new char[blockSize];
                    var totalRead = sr.ReadBlock(buf, 0, blockSize);
                    sb.Append(buf);
                    if (DateTime.Now - startDownloadTime > TimeSpan.FromSeconds(1))
                    {
                        startDownloadTime = DateTime.Now;
                        logger.WriteEntry(string.Format("Downloaded {0:#,##0} bytes so far", sb.Length), LogType.Info);
                    }                    
                }
                return sb.ToString();
            }
        }

        private string GetScheduleJSON(DateTime startDate, ILogger logger)
        {
            return GetJSON("getscheduale", "startdate=" + startDate.ToString("yyyyMMdd"), logger);
        }

        private string GetChannelsJSON(ILogger logger)
        {
            return GetJSON("getchannels", null, logger);
        }

        private static void SetupDescription(Show s)
        {
            if (!string.IsNullOrEmpty(s.Description))
            {
                if (s.Description.EndsWith(" ש.ח."))
                {
                    s.Title = string.Format("{0} - ש.ח", s.Title);
                }
                if (s.Description.EndsWith(" שידור חי") || s.Description.EndsWith(" שידור חי."))
                {
                    s.Title = string.Format("{0} - שידור חי", s.Title);
                }
            }
        }
    }
}
