using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using XmlTvGenerator.Core;

namespace dmi.ae
{
    public enum Channel
    {
        Channel1 = 2,
        Channel2 = 22,
        Channel3 = 23,
        Channel4 = 24,
        ChannelHD = 25,
    }

    public class WebSiteGrabber : GrabberBase
    {
        const string urlFormat = "http://www.dmi.ae/dubaisports/schedule.asp?lang=en&PrgDate={0}&ChannelID={1}";

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();

            foreach (Channel c in Enum.GetValues(typeof(Channel)))
            {
                var d = DateTime.Now.Date;
                var wr = WebRequest.Create(string.Format(urlFormat, d.ToString("dd/MM/yyyy"), (int)c));
                var res = (HttpWebResponse)wr.GetResponse();
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.Load(res.GetResponseStream(),Encoding.UTF8);

                var tbl = doc.DocumentNode.Descendants("table").FirstOrDefault(x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "omega_mt1 table1");
                var lst = new List<Show>();
                DateTime? lastStartTime = null;
                foreach (var tr in tbl.Descendants("tr").Where(x => x.Attributes["class"].Value == "bg3" || x.Attributes["class"].Value == "bg5"))
                {
                    var show = new Show();
                    var gmtTime = Tools.CleanupText(tr.Descendants("td").ToList()[2].InnerText);
                    var startTime = Convert.ToDateTime(gmtTime);
                    show.StartTime = DateTime.SpecifyKind(d.Date + Convert.ToDateTime(startTime).TimeOfDay, DateTimeKind.Unspecified);
                    show.StartTime = TimeZoneInfo.ConvertTime(show.StartTime, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"), TimeZoneInfo.Utc);
                    if (lastStartTime.HasValue && lastStartTime.Value > show.StartTime)
                    {
                        d = d.AddDays(1);
                        show.StartTime = show.StartTime.AddDays(1);
                    }
                    lastStartTime = show.StartTime;
                    show.Title = Tools.CleanupText(tr.Descendants("td").ToList()[3].ChildNodes[1].InnerText);
                    show.Description = Tools.CleanupText(tr.Descendants("td").ToList()[3].ChildNodes[2].InnerText);
                    show.Channel = c.ToString();
                    lst.Add(show);
                }
                FixShowsEndTimeByStartTime(lst);
                shows.AddRange(lst);
            }
            return shows;
        }
    }
}
