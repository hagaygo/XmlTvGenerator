using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using XmlTvGenerator.Core;

namespace AlJazeera.Sports
{
    public class WebSiteGrabber : GrabberBase
    {
        const string urlFormat = "http://www.aljazeerasport.net/Services/Schedule/EPGWeekSchedule.aspx?channelCode={0}&lang=0";

        List<Show> Grab(GrabParametersBase p)
        {
            var shows = new List<Show>();
            try
            {
                var param = (GrabParameters)p;
                var wr = WebRequest.Create(string.Format(urlFormat, (int)param.ChannelId));
                Console.WriteLine("Grabbing Channel {0} ...", param.ChannelId);
                var res = (HttpWebResponse)wr.GetResponse();
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.Load(res.GetResponseStream());
                doc.OptionOutputAsXml = true;
                var writer = new StringWriter();
                doc.Save(writer);

                var xml = XDocument.Load(new StringReader(writer.ToString()));
                FillShows(xml, shows);
                for (int i = shows.Count - 1; i >= 0; i--)
                {
                    var show = shows[i];
                    show.Channel = param.ChannelId.ToString();
                    if (i == shows.Count - 1)
                        show.EndTime = show.StartTime.AddHours(12);// usually 3-4 days from now , not that important
                    else
                        show.EndTime = shows[i + 1].StartTime;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("Found {0} Shows", shows.Count);
            return shows;
        }

        private void FillShows(XDocument xml, List<Show> shows)
        {
            var body = xml.Element("span").Element("html").Element("body");
            var table = body.Element("table");
            bool firstRow = true;
            foreach (var row in table.Elements("tr"))
            {
                if (firstRow)
                {
                    firstRow = false;
                    continue;
                }
                var show = new Show();
                var cells = row.Elements("td").ToList();
                show.Title = cells[0].Value;
                show.Description = cells[1].Value;
                show.StartTime = Convert.ToDateTime(cells[5].Value);
                show.StartTime += Convert.ToDateTime(cells[3].Value).TimeOfDay;
                show.StartTime = DateTime.SpecifyKind(show.StartTime, DateTimeKind.Unspecified);
                show.StartTime = TimeZoneInfo.ConvertTime(show.StartTime, TimeZoneInfo.FindSystemTimeZoneById("Arabic Standard Time"), TimeZoneInfo.Utc);
                shows.Add(show);
            }
        }

        public override List<Show> Grab(string xmlParameters)
        {
            // no xml parameter support for now
            var shows = new List<Show>();            
            foreach (ChannelEnum jscChannel in Enum.GetValues(typeof(ChannelEnum)))
            {
                var p = new GrabParameters();
                p.ChannelId = jscChannel;
                shows.AddRange(Grab(p));
            }

            return shows;
        }
    }
}
