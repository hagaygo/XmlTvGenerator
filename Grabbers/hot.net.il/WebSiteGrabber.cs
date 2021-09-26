using HtmlAgilityPack.CssSelectors.NetCore;
using System;
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
        Channel20 = 766,
        Kan11 = 839,
        Keshet12 = 861,
        Reshet13 = 863,
        Channel24 = 578,
        ShoppingChannel21 = 574,
        Makan33 = 841,
        I24English = 907,
        France24 = 468,
        BBCWorldNews = 441,
        Cnn = 451,
        Bloomberg = 443,
        DwNews = 771,
        Euronews = 335,
        Hot3 = 477,
        IsraelPlus = 431,
        HomePlus = 567,
        One1 = 685,
        Sport1 = 580,
        Sport2 = 581,
        Sport3 = 826,
        Sport4 = 828,
        Sport5 = 582,
        Sport5Plus = 583,
        Sport5Gold = 428,
        Sport5PlusLive = 429,
        EuroSport1 = 461,
        EuroSport2 = 673,
        One2 = 912,
        Hop = 591,
        HopYaldut = 592,
        DisneyJunior = 751,
        Kidz = 871,
        Disney = 566,
        Zoom = 738,
        Nick = 579,
        NickJunior = 710,
        Junior = 513,
        TeenNick = 792,
        Sport5HD = 544,
        SkyNews = 542,
        FoxNews = 465,
        FoxBusiness = 791,
        Mtv = 522
    }

    class ChannelData
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IncludeDescription { get; set; }
    }

    public class WebSiteGrabber : GrabberBase
    {
        const int PageSize = 100;

        const string url = "http://www.hot.net.il/PageHandlers/LineUpAdvanceSearch.aspx?text=&channel={0}&genre=-1&ageRating=-1&publishYear=-1&productionCountry=-1&startDate={1}&endDate={2}&currentPage=1&pageSize={3}&isOrderByDate=true&lcid=1037&pageIndex=1";
        const string DateFormat = "dd/MM/yyyy";

        protected override bool UseGenericDataDictionary => true;

        string getUrl(string channelId, int startDateDiff, int endDateDays, int pageSize)
        {
            return string.Format(url, channelId, DateTime.Now.Date.AddDays(startDateDiff).ToString(DateFormat), DateTime.Now.Date.AddDays(endDateDays).ToString(DateFormat), pageSize);
        }

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            var shows = new List<Show>();
            var doc = XDocument.Parse(xmlParameters);
            var sdElement = doc.Descendants("StartDate").FirstOrDefault();
            var startDateDiff = sdElement != null && sdElement.Value != null ? Convert.ToInt32(sdElement.Value) : -1;
            var edElement = doc.Descendants("EndDate").FirstOrDefault();
            var endDateDays = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;            

            var channelDict = new Dictionary<string, ChannelData>();

            foreach (Channel c in Enum.GetValues(typeof(Channel)))
            {
                channelDict.Add(((int)c).ToString(), new ChannelData { Code = ((int)c).ToString(), Name = c.ToString(), IncludeDescription = false });
            }

            var extraChannels = doc.Descendants("ExtraChannels").FirstOrDefault();
            if (extraChannels != null)
            {
                if (string.Compare(extraChannels.Attribute("IgnoreInternal")?.Value,"true",true) == 0)
                    channelDict.Clear();
                foreach (var chan in extraChannels.Descendants("Channel"))
                {                    
                    channelDict.Add(chan.Attribute("ID").Value, new ChannelData { Code = chan.Attribute("ID").Value, Name = chan.Value, IncludeDescription = chan.Attribute("Description") != null ? chan.Attribute("Description").Value == "yes" : false });
                }                
            }

            var needDescriptionFetch = false;

            foreach (var c in channelDict.Keys)
            {
                var ps = PageSize;
                var channelData = channelDict[c];
                var channelName = channelData.Name;                

                while (ps > 0)
                {
                    var wr = WebRequest.Create(getUrl(c, startDateDiff, endDateDays, ps));
                    wr.Timeout = 30000;
                    logger.WriteEntry(string.Format("Grabbing hot.net.il channel {0} , page size = {1}", channelName, ps), LogType.Info);
                    var res = (HttpWebResponse)wr.GetResponse();

                    var html = new HtmlAgilityPack.HtmlDocument();
                    html.Load(res.GetResponseStream(), Encoding.UTF8);

                    if (html.DocumentNode.InnerHtml.StartsWith("<p id=\"noData\""))
                    {
                        logger.WriteEntry(string.Format("no Data for page size = {1} , Regrabing hot.net.il channel {0} ", channelName, ps), LogType.Info);
                        if (ps <= 20)
                            ps -= 10;
                        else
                            ps -= 20;                        
                        continue;
                    }                    

                    foreach (var tr in html.DocumentNode.Descendants("tr").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "redtr_off"))
                    {
                        var tds = tr.Descendants("td").ToList();                        
                        var show = new Show();
                        try
                        {
                            show.Title = tds[2].InnerText.Trim();
                            var dateText = tds[4].InnerText;
                            if (dateText.Contains(","))
                                dateText = dateText.Substring(dateText.IndexOf(",") + 1).Trim();
                            show.StartTime = DateTime.SpecifyKind(Convert.ToDateTime(dateText), DateTimeKind.Unspecified);
                            show.StartTime = TimeZoneInfo.ConvertTime(show.StartTime, TimeZoneInfo.Local, TimeZoneInfo.Utc);
                            show.EndTime = show.StartTime.Add(Convert.ToDateTime(tds[5].InnerText).TimeOfDay);
                            show.Channel = channelName;
                            show.Description = string.Empty;
                            if (channelData.IncludeDescription)
                            {
                                try
                                {
                                    var url = tr.GetAttributeValue("onclick", string.Empty);
                                    if (url.Length > 0)
                                    {
                                        url = url.Substring(url.IndexOf("=") + 2);
                                        url = url.Remove(url.Length - 1);
                                        show.Description = url;
                                        needDescriptionFetch = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.WriteEntry("description fetch failed for " + dateText + " message : " + ex.Message, LogType.Error);
                                }
                            }
                            shows.Add(show);
                        }
                        catch (ArgumentException ex)
                        {
                            logger.WriteEntry(ex.Message, LogType.Error);
                        }
                    }                    

                    break;
                }                
            }

            if (needDescriptionFetch)
            {
                var lst = shows.Where(x => x.Description.Length > 0).ToList();
                var lck = new object();
                Parallel.ForEach(lst, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (s) =>
                {
                    var id = s.Description;
                    var url = "https://www.hot.net.il" + id;
                    var gotCache = false;
                    lock (lck)
                    {
                        if (GenericDictionary.TryGetValue(s.Description, out var desc))
                        {
                            s.Description = desc;
                            gotCache = true;
                        }                   
                        else
                        logger.WriteEntry("fetching description for " + url, LogType.Info);
                    }
                    if (!gotCache)
                    {
                        s.Description = GetDescription(url, logger);
                        lock (lck)
                        {
                            GenericDictionary[id] = s.Description;
                        }
                    }
                });
            }

            return shows;
        }

        private string GetDescription(string url, ILogger logger)
        {
            var wr = WebRequest.Create(url);            
            var res = (HttpWebResponse)wr.GetResponse();
            var html = new HtmlAgilityPack.HtmlDocument();
            html.Load(res.GetResponseStream(), Encoding.UTF8);
            var div = html.QuerySelector("div.widgetHPTitle");
            var desc = System.Web.HttpUtility.HtmlDecode(div.NextSiblingElement().Descendants("td").First().InnerText).Trim();
            return desc;
        }
    }
}
