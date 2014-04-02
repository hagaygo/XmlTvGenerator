using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace beinsports.net
{
    public class WebSiteGrabber : GrabberBase
    {
        const string URL = "http://www.en.beinsports.net/ajax/schedule/date/{0}";
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

                var wr = WebRequest.Create(string.Format(URL, date.ToString(DateFormat)));
                logger.WriteEntry(string.Format("Grabbing bein sports date {0} ...", date.ToString(DateFormat)), LogType.Info);
                var res = (HttpWebResponse)wr.GetResponse();

                var data = XDocument.Load(res.GetResponseStream());
                var html = new HtmlAgilityPack.HtmlDocument();
                html.LoadHtml(data.Descendants("body").First().Value);

                var dataDivs = html.DocumentNode.Descendants("div").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value.StartsWith("gr-10_5 carousel")).ToList();
                int divCounter = 0;
                foreach (var div in html.DocumentNode.Descendants("div").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value.StartsWith("gr-0_5 cell-channel channels")))
                {
                    var classes = div.Attributes["class"].Value.Split(' ');
                    var channel = classes[classes.Length - 1];
                    if (dataDivs.Count > 0)
                    {
                        foreach (var li in dataDivs[divCounter].Descendants("li"))
                        {
                            var time = li.Descendants("time").FirstOrDefault();
                            if (time == null)
                                continue;
                            var show = new Show();
                            show.Channel = channel;
                            show.StartTime = Convert.ToDateTime(time.Attributes["datetime"].Value);
                            show.StartTime = DateTime.SpecifyKind(show.StartTime, DateTimeKind.Utc);                            
                            var dvText = li.Descendants("div").First();
                            show.Title = dvText.ChildNodes[0].InnerText.Trim();
                            var spans = dvText.Descendants("span").ToList();
                            if (spans.Count > 0)
                                show.Description = spans[0].InnerText.Trim();
                            shows.Add(show);
                        }
                    }
                    divCounter++;
                }
            }
            var lst = new List<Show>();
            foreach (var chan in shows.Select(x => x.Channel).Distinct())
            {
                var channelShows = shows.Where(x => x.Channel == chan).OrderBy(x => x.StartTime).ToList();
                FixShowsEndTimeByStartTime(channelShows);
                lst.AddRange(channelShows);
            }
            return lst;
        }
    }
}
