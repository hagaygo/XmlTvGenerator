﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace hot.net.il
{
    public enum Channel
    {
        Kan11 = 869,        
        Keshet12 = 861,
        Reshet13 = 863,        
        Channel20 = 766,
        Channel24 = 578,
        Makan33 = 841,
        I24English = 907,
        France24 = 468,
        BBCWorldNews = 441,
        Cnn = 451,
        Bloomberg = 443,        
        DwNews = 771,
        Euronews = 335,
    }

    public class WebSiteGrabber : GrabberBase
    {
        const string url = "http://www.hot.net.il/PageHandlers/LineUpAdvanceSearch.aspx?text=&channel={0}&genre=-1&ageRating=-1&publishYear=-1&productionCountry=-1&startDate={1}&endDate={2}&currentPage=1&pageSize=100&isOrderByDate=true&lcid=1037&pageIndex=1";
        const string DateFormat = "dd/MM/yyyy";

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();
            var doc = XDocument.Parse(xmlParameters);
            var sdElement = doc.Descendants("StartDate").FirstOrDefault();
            var startDateDiff = sdElement != null && sdElement.Value != null ? Convert.ToInt32(sdElement.Value) : -1;
            var edElement = doc.Descendants("EndDate").FirstOrDefault();
            var endDateDays = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;

            foreach (Channel c in Enum.GetValues(typeof(Channel)))
            {
                var wr = WebRequest.Create(string.Format(url, (int)c, DateTime.Now.Date.AddDays(startDateDiff).ToString(DateFormat), DateTime.Now.Date.AddDays(endDateDays).ToString(DateFormat)));
                wr.Timeout = 30000;
                logger.WriteEntry(string.Format("Grabbing hot.net.il channel {0} ", c.ToString()), LogType.Info);
                var res = (HttpWebResponse)wr.GetResponse();

                var html = new HtmlAgilityPack.HtmlDocument();
                html.Load(res.GetResponseStream(), Encoding.UTF8);

                foreach (var tr in html.DocumentNode.Descendants("tr").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "redtr_off"))
                {
                    var tds = tr.Descendants("td").ToList();
                    var show = new Show();
                    show.Title = tds[2].InnerText;
                    var dateText = tds[4].InnerText;
                    if (dateText.Contains(","))
                        dateText = dateText.Substring(dateText.IndexOf(",") + 1).Trim();
                    show.StartTime = DateTime.SpecifyKind(Convert.ToDateTime(dateText),DateTimeKind.Unspecified);
                    show.StartTime = TimeZoneInfo.ConvertTime(show.StartTime, TimeZoneInfo.Local, TimeZoneInfo.Utc);
                    show.EndTime = show.StartTime.Add(Convert.ToDateTime(tds[5].InnerText).TimeOfDay);
                    show.Channel = c.ToString();
                    shows.Add(show);
                }
            }
            return shows;
        }
    }
}
