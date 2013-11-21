using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using XmlTvGenerator.Core;
using System.Xml.Linq;

namespace BBC.World.News
{
    public class WebSiteGrabber : GrabberBase
    {
        const string URL = "http://www.bbcworldnews.com/Pages/SchedulesByFormats.aspx?TimeZone=308&StartDate={0}&EndDate={1}&Format=Text";

        List<Show> Grab(GrabParametersBase p)
        {
            var pp = (GrabParameters)p;

            var url = string.Format(URL, pp.FromDate.ToString("dd/MM/yyyy"), pp.ToDate.ToString("dd/MM/yyyy"));

            var wr = WebRequest.Create(url);
            _logger.WriteEntry("Grabbing BBCW", LogType.Info);
            var res = (HttpWebResponse)wr.GetResponse();
            using (var sr = new StreamReader(res.GetResponseStream()))
            {
                var lst = new List<Show>();
                sr.ReadLine(); // first line
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (!string.IsNullOrEmpty(line) && line.Length > 10)
                    {
                        var show = new Show();
                        show.Channel = "BBC World News";
                        var tokens = line.Split('\t');
                        show.StartTime = DateTime.SpecifyKind(Convert.ToDateTime(tokens[0]) + Convert.ToDateTime(tokens[1]).TimeOfDay, DateTimeKind.Unspecified);
                        show.StartTime = TimeZoneInfo.ConvertTime(show.StartTime, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"), TimeZoneInfo.Utc);
                        show.Title = tokens[2];
                        //show.Episode = string.IsNullOrEmpty(tokens[3]) ? null : (int?)Convert.ToInt32(tokens[3]); // not number
                        show.Description = tokens[4];
                        lst.Add(show);
                    }
                }
                return lst;
            }
        }

        ILogger _logger;

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            _logger = logger;
            var doc = XDocument.Parse(xmlParameters);
            var startDayDiffElement = doc.Descendants("StartDate").FirstOrDefault();
            var endDayDiffElement = doc.Descendants("EndDate").FirstOrDefault();
            var startDayDiff = startDayDiffElement != null ? Convert.ToInt32(startDayDiffElement.Value) : 0;
            var endDayDiff = endDayDiffElement != null ? Convert.ToInt32(endDayDiffElement.Value) : 3;

            var p = new GrabParameters();
            p.FromDate = DateTime.Now.Date.AddDays(startDayDiff);
            p.ToDate = DateTime.Now.Date.AddDays(endDayDiff);
            var shows = new List<Show>();
            shows.AddRange(Grab(p));
            FixShowsEndTimeByStartTime(shows);
            return shows;
        }
    }
}
