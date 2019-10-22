using System;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace Sport5.co.il
{
    public class WebSiteGrabber : GrabberBase
    {
        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            logger.WriteEntry("Grabbing Sport5.co.il ...", LogType.Info);
            var shows = new List<Show>();
            try
            {
                var wr = WebRequest.Create("https://xml.sport5.co.il/ExternalAPI/GetBroadcastRadio.aspx");                
                var res = (HttpWebResponse)wr.GetResponse();
                var xml = XDocument.Load(res.GetResponseStream());
                foreach (var item in xml.Descendants("item"))
                {
                    var s = new Show();
                    s.Channel = "Sport5";                    
                    s.StartTime = DateTime.Parse(item.Element("Date").Value + " " + item.Element("BroadcastHour").Value);
                    s.StartTime = TimeZoneInfo.ConvertTime(s.StartTime, TimeZoneInfo.Local, TimeZoneInfo.Utc);
                    s.Description = item.Element("BroadcastDesc").Value;
                    s.Title = item.Element("BroadcastName").Value;
                    if (item.Element("Live").Value == "1")
                        s.Title += " (ישיר)";
                    shows.Add(s);
                }                
            }
            catch (Exception ex)
            {
                logger.WriteEntry(ex.Message, LogType.Error);
            }
            logger.WriteEntry(string.Format("Found {0} Shows", shows.Count), LogType.Info);
            FixShowsEndTimeByStartTime(shows);
            return shows;
        }
    }
}
