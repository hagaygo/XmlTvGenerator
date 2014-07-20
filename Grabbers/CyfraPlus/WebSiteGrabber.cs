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
        const string urlFormat = "http://ncplus.pl/~/epgjson/{0}.ejson";
        const string DateFormat = "yyyy-MM-dd";



        List<Show> Grab(GrabParametersBase p,ILogger logger)
        {
            var pp = (CyfraPlus.GrabParameters)p;
            var shows = new List<Show>();
            var wr = WebRequest.Create(string.Format(urlFormat, pp.Date.ToString(DateFormat)));
            logger.WriteEntry(string.Format("Grabbing Cyfra+ date {0} ...", pp.Date.ToString(DateFormat)), LogType.Info);
            var res = (HttpWebResponse)wr.GetResponse();            
            const int ChannelDepth = 2;

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
                
                var r = new Newtonsoft.Json.JsonTextReader(new StringReader(data.ToString()));

                while (r.Read())
                {
                    r.Read();
                    var channelNumber = r.ReadAsInt32();
                    var channelName = r.ReadAsString();
                    r.Read();
                    r.Read();
                    while (r.Depth > ChannelDepth)
                    {
                        var show = new Show();
                        show.Channel = channelName.Trim();
                        var programId = r.ReadAsInt32();
                        show.Title = Tools.CleanupText(r.ReadAsString());

                        show.StartTime = new DateTime(1970, 1, 1).Add(TimeSpan.FromSeconds(r.ReadAsInt32().Value));                        
                        show.EndTime = show.StartTime.Add(TimeSpan.FromSeconds(Convert.ToDouble(r.ReadAsInt32())));
                        var num = r.ReadAsInt32();
                        shows.Add(show);
                        r.Read();
                        r.Read();
                        r.Read();                        
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
            for (int i = startDateDiff; i < endDateDays; i++)
            {
                var p = new GrabParameters() { Date = DateTime.Now.Date.AddDays(i) };
                shows.AddRange(Grab(p, logger));
            }
            return shows;
        }
    }

    public class GrabParameters : GrabParametersBase
    {
        public DateTime Date { get; set; }
    }
}
