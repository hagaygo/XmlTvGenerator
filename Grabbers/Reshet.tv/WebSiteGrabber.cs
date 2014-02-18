using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using XmlTvGenerator.Core;
using System.Xml.Linq;
using System.IO;

namespace Reshet.tv
{
    public class WebSiteGrabber: GrabberBase
    {
        const string Url = "http://reshet.tv//Handlers/TvGuide.ashx/GetTvGuideOpenPerDate";        
 
        const string DateFormat = "dd/MM/yyyy";
        
        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();
            var doc = XDocument.Parse(xmlParameters);
            var sdElement = doc.Descendants("StartDate").FirstOrDefault();
            var startDateDiff = sdElement != null && sdElement.Value != null ? Convert.ToInt32(sdElement.Value) : -1;
            var edElement = doc.Descendants("EndDate").FirstOrDefault();
            var endDateDays = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;
            
            for (int i =0; i <= endDateDays; i++)
            {
                var date = DateTime.Now.Date.AddDays(i);
                logger.WriteEntry(string.Format("Grabbing reshet.tv for date {0}", date.ToString("d")), LogType.Info);
                var wr = WebRequest.Create(Url);
                wr.Method = "POST";
                wr.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                using (var sw = new StreamWriter(wr.GetRequestStream()))
                {
                    sw.Write(string.Format("Values={0}%2F{1}%2F{2}",date.Day.ToString("00"),date.Month.ToString("00"),date.Year));
                }
                var res = (HttpWebResponse)wr.GetResponse();
                var html = new HtmlAgilityPack.HtmlDocument();
                html.Load(res.GetResponseStream(),Encoding.UTF8);                
                foreach (var li in html.DocumentNode.Descendants("li"))
                {
                    var time = li.Descendants("span").First().InnerText;
                    var text = li.Descendants("p").First().InnerText;
                }
            }
            return shows;
        }
    }
}
