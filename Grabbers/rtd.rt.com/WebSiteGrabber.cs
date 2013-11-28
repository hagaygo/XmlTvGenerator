using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace rtd.rt.com
{
    public class WebSiteGrabber : GrabberBase
    {
        const string url = "http://rtd.rt.com/schedule/{0}/";
        const string DateFormat = "yyyy-MM-dd";

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();

            var doc = XDocument.Parse(xmlParameters);
            var sdElement = doc.Descendants("StartDate").FirstOrDefault();
            var startDateDiff = sdElement != null && sdElement.Value != null ? Convert.ToInt32(sdElement.Value) : -1;
            var edElement = doc.Descendants("EndDate").FirstOrDefault();
            var endDateDays = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;
            for (int i = startDateDiff; i <= endDateDays; i++)
            {
                var date = DateTime.Now.Date.AddDays(i);
                var wr = WebRequest.Create(string.Format(url, date.ToString(DateFormat)));
                logger.WriteEntry(string.Format("Grabbing rtd date {0} ...", date.ToString(DateFormat)), LogType.Info);
                var res = (HttpWebResponse)wr.GetResponse();

                var html = new HtmlAgilityPack.HtmlDocument();
                html.Load(res.GetResponseStream());

                var divs = html.DocumentNode.Descendants("div").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value.EndsWith("b-lenta-film_one"));
                foreach (var div in divs)
                {
                    var show = new Show();
                    var titleA = div.Descendants("a").First(x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "b-film_heading");
                    show.Title = titleA.InnerText;
                    show.Channel = "RT DOC";

                    var groupA = div.Descendants("div").FirstOrDefault(x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "b-film_group");
                    if (groupA != null)
                    {
                        var groupText = groupA.Descendants("a").First().InnerText;
                        show.Description = show.Title;
                        show.Title = groupText;
                    }
                    show.StartTime = DateTime.SpecifyKind(date.AddMinutes(Convert.ToInt32(div.Attributes["class"].Value.Split(' ')[1].Substring(5))), DateTimeKind.Unspecified);
                    show.StartTime = TimeZoneInfo.ConvertTime(show.StartTime, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"), TimeZoneInfo.Utc);
                    shows.Add(show);
                }
            }
            FixShowsEndTimeByStartTime(shows);
            return shows;   
        }
    }
}
