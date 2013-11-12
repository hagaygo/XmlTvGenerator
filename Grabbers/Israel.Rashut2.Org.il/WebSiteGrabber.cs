using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using XmlTvGenerator.Core;

namespace Israel.Rashut2.Org.il
{
    public class WebSiteGrabber: GrabberBase
    {
        const string Channel2Url = "http://www.rashut2.org.il/broadcast_list.asp?PrmSearchDate={0}";
        const string Channel10Url = "http://www.rashut2.org.il/broadcast_10_list.asp?PrmSearchDate={0}";
 
        const string DateFormat = "dd/MM/yyyy";

        public override List<Show> Grab(GrabParametersBase p)
        {
            var pp = (GrabParameters)p;

            var url = GetUrl(pp);
            Console.WriteLine("Grabbing rashut2 {0} for date {1}", pp.Channel, pp.Date.ToString("d"));
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
            if (pp.Channel == Channel.Rashut2Channel10)
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
                if (d.Hour < 6)
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
                case Channel.Rashut2Channel2: return Channel2Url;
                case Channel.Rashut2Channel10: return Channel10Url;
                default: throw new ApplicationException("unknown rashut2 channel");
            }
        }
    }
}
