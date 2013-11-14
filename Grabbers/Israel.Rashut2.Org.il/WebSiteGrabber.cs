using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using XmlTvGenerator.Core;
using System.Xml.Linq;
using System.IO;

namespace Israel.Rashut2.Org.il
{
    public class WebSiteGrabber: GrabberBase
    {
        const string Channel2Url = "http://www.rashut2.org.il/broadcast_list.asp?PrmSearchDate={0}";
        const string Channel10Url = "http://www.rashut2.org.il/broadcast_10_list.asp?PrmSearchDate={0}";
 
        const string DateFormat = "dd/MM/yyyy";

        ILogger _logger;

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            _logger = logger;
            var lst = new List<Show>();
            var doc = XDocument.Load(new StringReader(xmlParameters), LoadOptions.None);
            var channels = new List<Channel>();
            foreach (var c in doc.Descendants("Channel"))
            {
                channels.Add((Channel)Enum.Parse(typeof(Channel), c.Value));
            }
            var sdElement = doc.Descendants("StartDate").FirstOrDefault();
            var startDate = sdElement != null && sdElement.Value != null ? DateTime.Now.Date.AddDays(Convert.ToInt32(sdElement.Value)) : DateTime.Now.Date.AddDays(-1);
            var edElement = doc.Descendants("EndDate").FirstOrDefault();
            var endDateDays = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;

            foreach (var c in channels)
                for (int i = 0; i < endDateDays; i++)
                {
                    var d = startDate.AddDays(i);
                    lst.AddRange(Grab(new GrabParameters() { Date = d, Channel = c }));
                }
            return lst;
        }

        List<Show> Grab(GrabParametersBase p)
        {
            var pp = (GrabParameters)p;

            var url = GetUrl(pp);
            _logger.WriteEntry(string.Format("Grabbing rashut2 {0} for date {1}", pp.Channel, pp.Date.ToString("d")), LogType.Info);
            var wr = WebRequest.Create(string.Format(url, pp.Date.ToString(DateFormat)));            
            var res = (HttpWebResponse)wr.GetResponse();
            var doc = new HtmlAgilityPack.HtmlDocument();            
            doc.Load(res.GetResponseStream());

            var nodes = doc.DocumentNode.SelectNodes("//comment()");
            if (nodes != null)
            {
                foreach (HtmlAgilityPack.HtmlNode comment in nodes)
                {
                    if (!comment.InnerText.StartsWith("DOCTYPE"))
                        comment.ParentNode.RemoveChild(comment);
                }
            }

            int cellDelta = 0;            
            var tbl = doc.GetElementbyId("table5");
            if (pp.Channel == Channel.Channel10)
            {
                tbl = doc.GetElementbyId("table1");
                cellDelta = 1;
            }

            const int childStart = 9;
            var shows = new List<Show>();
            for ( int i = childStart; i < tbl.ChildNodes.Count; i+=2)
            {
                var show = new Show();
                var d = Convert.ToDateTime(Tools.CleanupText(tbl.ChildNodes[i].ChildNodes[1].InnerText));
                show.StartTime = pp.Date.AddHours(d.Hour).AddMinutes(d.Minute);
                if (d.Hour < 6) // data is shown from 6 (AM) till next day 6 (AM) so after midnight we need to increase the date
                    show.StartTime = show.StartTime.AddDays(1);
                show.StartTime = TimeZoneInfo.ConvertTimeToUtc(show.StartTime, TimeZoneInfo.Local);
                show.Title = Tools.CleanupText(tbl.ChildNodes[i].ChildNodes[3 + cellDelta].InnerText);
                var episodeName = Tools.CleanupText(tbl.ChildNodes[i].ChildNodes[5 + cellDelta].InnerText);
                var episodeNumber = Tools.CleanupText(tbl.ChildNodes[i].ChildNodes[7 + cellDelta].InnerText);
                var genere = Tools.CleanupText(tbl.ChildNodes[i].ChildNodes[9 + cellDelta].InnerText);
                show.Description = string.Empty;
                if (!string.IsNullOrEmpty(episodeName))                
                    show.Description += string.Format("שם הפרק : {0}\n", episodeName);
                if (!string.IsNullOrEmpty(episodeNumber))
                {
                    show.Description += string.Format("מספר הפרק : {0}\n", episodeNumber);
                    int num;
                    if (int.TryParse(episodeNumber, out num))
                        show.Episode = num;
                }
                if (!string.IsNullOrEmpty(genere))
                    show.Description += string.Format("סוג תכנית : {0}\n", genere);                
                shows.Add(show);
            }

            for (int i = shows.Count - 1; i >= 0; i--)
            {
                var show = shows[i];
                if (show.Description != null)
                    show.Description = show.Description.Trim();
                show.Channel = pp.Channel.ToString();
                if (i == shows.Count - 1)
                    show.EndTime = show.StartTime.AddHours(1);
                else
                    show.EndTime = shows[i + 1].StartTime;
            }
            

            return shows;
        }

        private string GetUrl(GrabParameters pp)
        {
            switch (pp.Channel)
            {
                case Channel.Channel2: return Channel2Url;
                case Channel.Channel10: return Channel10Url;
                default: throw new ApplicationException("unknown rashut2 channel");
            }
        }
    }
}
