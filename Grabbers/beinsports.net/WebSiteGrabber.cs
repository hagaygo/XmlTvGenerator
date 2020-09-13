using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace beinsports.net
{
    public class WebSiteGrabber : GrabberBase
    {
        //https://epg.beinsports.com/utctime_au.php?cdate=2020-09-13&offset=0&mins=00&category=sports&id=123
        //https://epg.beinsports.com/utctime.php?cdate=2020-09-13&offset=0&mins=00&category=sports&id=123

        const string URL = "https://epg.beinsports.com/utctime{0}.php?cdate={1}&offset=0&mins=00&category=sports&id=123";
        const string DateFormat = "yyyy-MM-dd";

        List<string> regions = new List<string> { "", "_au" };

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();

            var doc = XDocument.Parse(xmlParameters);
            var sdElement = doc.Descendants("StartDate").FirstOrDefault();
            var startDateDiff = sdElement != null && sdElement.Value != null ? Convert.ToInt32(sdElement.Value) : 0;
            var edElement = doc.Descendants("EndDate").FirstOrDefault();
            var endDateDays = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;
            for (int i = startDateDiff; i <= endDateDays; i++)
            {
                var date = DateTime.Now.Date.AddDays(i);
                foreach (var region in regions)
                {
                    try
                    {
                        var wr = WebRequest.Create(string.Format(URL, region, date.ToString(DateFormat)));
                        logger.WriteEntry(string.Format("Grabbing bein sports region {0} , date {1} ...", region, date.ToString(DateFormat)), LogType.Info);
                        var res = (HttpWebResponse)wr.GetResponse();
                        var html = new HtmlAgilityPack.HtmlDocument();
                        using (var data = res.GetResponseStream())
                        {
                            html.Load(data);
                            var channelsDivs = html.QuerySelectorAll("div.container div div[id^=channels_]");
                            foreach (var channelDiv in channelsDivs)
                            {
                                var img = channelDiv.QuerySelector("div.channel div.centered img");
                                var src = img.Attributes["src"].Value;
                                src = src.Substring(src.IndexOf("/") + 1);
                                var channelName = src.Substring(0, src.IndexOf(".")) + region;

                                foreach (var li in channelDiv.QuerySelectorAll("div.slider ul li"))
                                {
                                    if (li.QuerySelector("p.title") != null)
                                    {
                                        var title = HttpUtility.HtmlDecode(li.QuerySelector("p.title").InnerText);
                                        var format = HttpUtility.HtmlDecode(li.QuerySelector("p.format").InnerText);
                                        var time = HttpUtility.HtmlDecode(li.QuerySelector("p.time").InnerText);
                                        var live = li.QuerySelectorAll("img.image_live_css").Count > 0;
                                        if (live)
                                            title = "Live - " + title;
                                        var startTime = time.Substring(0, time.IndexOf("-") - 1).Trim();
                                        var endTime = time.Substring(time.IndexOf("-") + 1).Trim();
                                        var show = new Show();
                                        show.Channel = channelName;
                                        show.Title = title;
                                        show.Description = format;
                                        show.StartTime = date + DateTime.Parse(startTime).TimeOfDay;
                                        show.StartTime = DateTime.SpecifyKind(show.StartTime, DateTimeKind.Utc);
                                        show.EndTime = date + DateTime.Parse(endTime).TimeOfDay;
                                        show.EndTime = DateTime.SpecifyKind(show.EndTime, DateTimeKind.Utc);
                                        if (show.StartTime > show.EndTime)
                                            show.StartTime = show.StartTime.AddDays(-1);
                                        shows.Add(show);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.WriteEntry(ex.Message + ex.StackTrace, LogType.Error);
                    }
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
