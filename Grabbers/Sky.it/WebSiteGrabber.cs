using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using TimeZoneConverter;
using XmlTvGenerator.Core;

namespace Sky.it
{
    public class WebSiteGrabber : GrabberBase
    {
        const string URL = "http://guidatv.sky.it/guidatv/canale/{0}.shtml";

        enum Day
        {
            dom = 0,
            lun = 1,
            mar = 2,
            mer = 3,
            gio = 4,
            ven = 5,
            sab = 6,
        }

        List<Show> Grab(GrabParametersBase p, ILogger logger)
        {
            var pp = (GrabParameters)p;
            var url = string.Format(URL, pp.Channel.ToString().Replace("_", "-").Replace("AANNDD", "%26").Replace("PPLLUUSS", "%2B"));
            var wr = WebRequest.Create(url);
            var res = (HttpWebResponse)wr.GetResponse();
            var doc = new HtmlAgilityPack.HtmlDocument();
            logger.WriteEntry(string.Format("Grabbing Channel {0}", pp.Channel), LogType.Info);
            doc.Load(res.GetResponseStream());
            var shows = new List<Show>();
            foreach (Day d in Enum.GetValues(typeof(Day)))
            {
                var dayOfWeek = (DayOfWeek)d;
                var div = doc.DocumentNode.Descendants("div").FirstOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value == d.ToString());
                if (div != null)
                {
                    var date = NextDateOfDayOfWeek(dayOfWeek);
                    foreach (var ul in div.Descendants("ul"))
                    {
                        foreach (var li in ul.Descendants("li"))
                        {
                            var par = li.Descendants("p").First();
                            var a = li.Descendants("a").First();
                            var show = new Show();
                            show.Channel = pp.Channel.ToString();
                            show.Title = a.InnerText.Trim();
                            show.StartTime = DateTime.SpecifyKind(date + Convert.ToDateTime(par.InnerText.Trim()).TimeOfDay, DateTimeKind.Unspecified);                            
                            show.StartTime = TimeZoneInfo.ConvertTime(show.StartTime, TZConvert.GetTimeZoneInfo("Central European Standard Time"), TimeZoneInfo.Utc);
                            shows.Add(show);
                        }
                    }
                }
            }            
            return shows;
        }

        private DateTime NextDateOfDayOfWeek(DayOfWeek dayOfWeek)
        {
            var d = DateTime.Now.Date;
            while (d.DayOfWeek != dayOfWeek)
            {
                d = d.AddDays(1);
            }
            return d;
        }

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            logger.WriteEntry("Started sky.it grab", LogType.Info);

            var selectedChannels = new List<Channel>();
            var shows = new List<Show>();                
            if (!string.IsNullOrEmpty(xmlParameters))
            {
                var doc = XDocument.Parse(xmlParameters);                
                foreach (var c in doc.Descendants("Channel"))
                    selectedChannels.Add((Channel)Enum.Parse(typeof(Channel), c.Value));
            }
            if (selectedChannels.Count == 0) // no channel specified , lets take all available channels
                foreach (Channel c in Enum.GetValues(typeof(Channel)))
                    selectedChannels.Add(c);

            foreach (var c in selectedChannels)
            {
                var p = new GrabParameters();
                p.Channel = c;
                var channelShows = Grab(p, logger).OrderBy(x => x.StartTime).ToList() ;
                FixShowsEndTimeByStartTime(channelShows);
                shows.AddRange(channelShows);
            }

            logger.WriteEntry("Finshed sky.it grab", LogType.Info);
            return shows;
        }
    }
}
