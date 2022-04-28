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

namespace CyfraPlus
{
    public class WebSiteGrabber : GrabberBase
    {        
        const string urlFormat = "https://pl.canalplus.com/iapi/tv-schedule/Schedule";
        const string DateFormat = "yyyy-MM-dd";

        List<Show> Grab(GrabParametersBase p, ILogger logger)
        {
            var pp = (CyfraPlus.GrabParameters)p;
            var shows = new List<Show>();
            var wr = WebRequest.Create(urlFormat);
            wr.Method = "POST";
            wr.ContentType = "application/json";
            using (var sw = new StreamWriter(wr.GetRequestStream()))
            {
                sw.Write("{\"start_date\":\"" + pp.Date.ToString(DateFormat) + "T00:00:00\",\"end_date\":\"" + pp.Date.AddDays(1).ToString(DateFormat) + "T00:00:00\"}");
            }
            logger.WriteEntry(string.Format("Grabbing Cyfra+ date {0} ...", pp.Date.ToString(DateFormat)), LogType.Info);
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
                var channels = (JArray)obj["data"];

                foreach (var channel in channels)
                {
                    var programs = channel["Programs"];
                    var channelName = _channelsDict[channel.Value<int>("channel_id")];
                    foreach (var program in programs)
                    {
                        var show = new Show();
                        show.Channel = channelName;
                        show.Title = Tools.CleanupText(program["name"].ToString());
                        show.StartTime = DateTime.Parse(program["start_date"].ToString()).ToUniversalTime();
                        show.EndTime = DateTime.Parse(program["end_date"].ToString()).ToUniversalTime();
                        shows.Add(show);
                    }

                }
            }
            return shows;
        }

        Dictionary<int, string> _channelsDict;

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();
            var doc = XDocument.Parse(xmlParameters);            
            var sdElement = doc.Descendants("StartDate").FirstOrDefault();
            var startDateDiff = sdElement != null && sdElement.Value != null ? Convert.ToInt32(sdElement.Value) : -1;
            var edElement = doc.Descendants("EndDate").FirstOrDefault();
            var endDateDays = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;

            _channelsDict = GetChannelsDict(logger);

            for (int i = startDateDiff; i < endDateDays; i++)
            {
                var p = new GrabParameters() { Date = DateTime.Now.Date.AddDays(i) };
                shows.AddRange(Grab(p, logger));
            }
            return shows;
        }

        private Dictionary<int, string> GetChannelsDict(ILogger logger)
        {
            var wr = WebRequest.CreateHttp("https://pl.canalplus.com/iapi/tv-schedule/AllChannels");
            try
            {
                var res = (HttpWebResponse)wr.GetResponse();

                var dict = new Dictionary<int, string>();
                var obj = JObject.Parse(new StreamReader(res.GetResponseStream()).ReadToEnd())["data"];
                foreach (var channel in obj)
                {
                    dict[channel.Value<int>("id")] = channel.Value<string>("name");
                }
                return dict;
            }
            catch (Exception ex)
            {
                logger.LogException(ex, "failed to get cyfra channels");
                throw;
            }
        }
    }

    public class GrabParameters : GrabParametersBase
    {
        public DateTime Date { get; set; }
    }
}
