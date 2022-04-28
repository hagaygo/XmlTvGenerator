using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using TimeZoneConverter;
using XmlTvGenerator.Core;

namespace streamingtvguides.com
{
    public enum Channels
    {
        fs1,
        fs2,
        cnn,
        cnni,
        nbcsn,
        msnbc,
        ESPN,
        ESPN2,
        NGWILD,
        NGC,
        SCIENCE,
        VH1,
        nbatv
    }

    public class WebSiteGrabber : GrabberBase
    {
        const string URL = "https://streamingtvguides.com/Channel/{0}";

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var channels = Enum.GetValues(typeof(Channels)).OfType<Object>().Select(x => x.ToString()).ToList();

            var doc = XDocument.Parse(xmlParameters);
            var channelsElement = doc.Descendants("Channels").FirstOrDefault();
            if (channelsElement != null)
            {
                channels = channelsElement.Elements().Where(x => x.Name == "Channel").Select(x => x.Value).ToList();
            }            

            var shows = new List<Show>();            

            foreach (var channel in channels)
            {
                try
                {
                    var wr = (HttpWebRequest)WebRequest.Create(string.Format(URL, channel));
                    wr.AllowAutoRedirect = false;
                    logger.WriteEntry(string.Format("Grabbing streamingtvguides {0} ...", channel), LogType.Info);
                    var res = (HttpWebResponse)wr.GetResponse();                    
                    var html = new HtmlAgilityPack.HtmlDocument();
                    using (var data = res.GetResponseStream())
                    {
                        html.Load(data);
                        var cards = html.QuerySelectorAll("div.card-body");
                        foreach (var card in cards)
                        {
                            var show = new Show();
                            show.Channel = channel.ToString();
                            show.Title = HttpUtility.HtmlDecode(card.QuerySelector(".card-title").InnerText).Trim();
                            show.Description = HttpUtility.HtmlDecode(card.QuerySelector(".card-text").InnerText).Trim();
                            var time = HttpUtility.HtmlDecode(card.ChildNodes.First(x => x is HtmlAgilityPack.HtmlTextNode && ((HtmlAgilityPack.HtmlTextNode)x).InnerText.Trim().Length > 0).InnerText).Trim();
                            var startTime = time.Substring(0, time.IndexOf(" - ")).Trim();
                            show.StartTime = GetDate(startTime);                            
                            var endTime = time.Substring(time.IndexOf(" - ") + 3).Trim();
                            show.EndTime = GetDate(endTime);                            

                            if (show.StartTime > show.EndTime)
                                show.StartTime = show.StartTime.AddDays(-1);
                            shows.Add(show);
                        }                        
                    }
                }
                catch (Exception ex)
                {
                    logger.LogException(ex);
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

        private DateTime GetDate(string startTime)
        {
            var tokens = startTime.Split(' ');
            var date = DateTime.Parse(tokens[0]);
            var time = DateTime.Parse(tokens[1]).TimeOfDay;
            var tz = TZConvert.GetTimeZoneInfo("Pacific Standard Time");
            var dt = DateTime.SpecifyKind(date + time, DateTimeKind.Unspecified);
            dt = TimeZoneInfo.ConvertTimeToUtc(dt, tz);                       
            return dt;
        }
    }
}
