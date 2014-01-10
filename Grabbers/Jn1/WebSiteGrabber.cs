using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using XmlTvGenerator.Core;

namespace Jn1
{
    public class WebSiteGrabber : GrabberBase
    {
        const string URL = "http://jn1.tv/schedule/";
                
        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            logger.WriteEntry("Grabbing jn1 schedule", LogType.Info);
            var wr = WebRequest.Create(URL);
            var res = (HttpWebResponse)wr.GetResponse();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(res.GetResponseStream());
            var lst = new List<Show>();
            var ul = doc.DocumentNode.Descendants("ul").FirstOrDefault(x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "program_list");
            foreach (var li in ul.Descendants("li"))
            {
                var show = new Show();
                show.Channel = "Jewish News One";
                var startTime = li.Descendants("span").First().InnerText.Replace("::", ":");
                show.StartTime = DateTime.SpecifyKind(DateTime.Now.Date + Convert.ToDateTime(startTime).TimeOfDay, DateTimeKind.Unspecified);
                show.StartTime = TimeZoneInfo.ConvertTime(show.StartTime, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"), TimeZoneInfo.Utc);
                show.Title = li.Descendants("span").ToList()[1].InnerText.Trim().ToLower();
                show.Title = show.Title.First().ToString().ToUpper() + String.Join("", show.Title.Skip(1));
                show.Description = li.Descendants("span").ToList()[2].InnerText.Trim();

                lst.Add(show);                
            }
            foreach (var show in lst)
            {
                var s = show.Clone(); // has same daily schedule every day , so just duplicate entries with tommorow date
                s.StartTime = show.StartTime.AddDays(1);
                lst.Add(s);
            }
            FixShowsEndTimeByStartTime(lst);
            return lst;
        }
    }

}
