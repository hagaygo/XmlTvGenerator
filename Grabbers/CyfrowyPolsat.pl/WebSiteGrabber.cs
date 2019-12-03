﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using TimeZoneConverter;
using XmlTvGenerator.Core;

namespace CyfrowyPolsat.pl
{
    public class WebSiteGrabber : GrabberBase
    {        
        const string URL = "https://www.cyfrowypolsat.pl/redir/program-tv/program-tv-pionowy-single-channel.cp?chN={0}";        

        DateTime currentNow;

        public List<Show> Grab(GrabParametersBase p, ILogger logger)
        {
            currentNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, TZConvert.GetTimeZoneInfo("Central European Standard Time"));

            var pp = (GrabParameters)p;
            logger.WriteEntry("grabbing CyfrowyPolsat.pl channel : " + pp.Channel, LogType.Info);
            var wr = (HttpWebRequest)WebRequest.Create(string.Format(URL, pp.Channel.ToString().Replace("_", "-")));
            wr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:49.0) Gecko/20100101 Firefox/49.0";
            wr.Timeout = 10000;
            var res = (HttpWebResponse)wr.GetResponse();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(res.GetResponseStream());
            var lst = new List<Show>();
            var div = doc.DocumentNode.Descendants("div").FirstOrDefault(x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "main col");
            var times = div.Descendants("span").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "time").ToList();
            var names = div.Descendants("a").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "name").ToList();
            var metas = div.Descendants("div").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "meta").ToList();

            for (int i = 0; i < times.Count; i++)
            {
                var s = new Show();
                s.Channel = pp.Channel.ToString();
                var startTime = Convert.ToDateTime(times[i].InnerText);
                s.StartTime = DateTime.SpecifyKind(currentNow.Date + Convert.ToDateTime(startTime).TimeOfDay, DateTimeKind.Unspecified);
                s.StartTime = TimeZoneInfo.ConvertTime(s.StartTime, TZConvert.GetTimeZoneInfo("Central European Standard Time"), TimeZoneInfo.Utc);
                if (lst.Count > 0 && s.StartTime <= lst[lst.Count - 1].StartTime)
                {
                    s.StartTime = s.StartTime.AddDays(1);
                    currentNow = currentNow.AddDays(1);
                }
                s.Title = HttpUtility.HtmlDecode(names[i].InnerText);
                s.Description = HttpUtility.HtmlDecode(metas[i * 2].InnerText + "\n" + metas[(i * 2) + 1].InnerText).Trim();
                lst.Add(s);
            }
            return lst;
        }

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            logger.WriteEntry("Started CyfrowyPolsat.pl grab", LogType.Info);

            var selectedChannels = new List<string>();
            var shows = new List<Show>();
            if (!string.IsNullOrEmpty(xmlParameters))
            {
                var doc = XDocument.Parse(xmlParameters);
                foreach (var c in doc.Descendants("Channel"))
                    selectedChannels.Add(c.Value);
            }
            if (selectedChannels.Count == 0) // no channel specified , lets take all available channels
                selectedChannels.AddRange(GrabParameters.AvailableChannels);                

            foreach (var c in selectedChannels)
            {
                var p = new GrabParameters();
                p.Channel = c;
                try
                {
                    var channelShows = Grab(p, logger);
                    FixShowsEndTimeByStartTime(channelShows);
                    shows.AddRange(channelShows);
                }
                catch (Exception ex)
                {
                    logger.WriteEntry("Error on grabber " + ex.Message, LogType.Error);
                }                
            }

            logger.WriteEntry("Finshed CyfrowyPolsat.pl grab", LogType.Info);
            return shows;
        }
    }
}
