using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace CellcomTV.il
{
    public class WebSiteGrabber : GrabberBase
    {
        const string URLBASE = "https://api.frp1.ott.kaltura.com/api_v3/service/";
        const string Useragent = "okhttp/3.12.1";
        const string ClientTag = "30082-Android";
        const string ApiVersion = "5.4.0.28193";
        const string PartnerId = "3197";
        const int PageSize = 500;
        ILogger _logger;

        class ChannelData
        {
            public string Name { get; set; }
            public int Id { get; set; }
        }

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            _logger = logger;
            var shows = new List<Show>();
            var doc = XDocument.Parse(xmlParameters);
            var edElement = doc.Descendants("DaysForward").FirstOrDefault();
            var daysForward = edElement != null && edElement.Value != null ? Convert.ToInt32(edElement.Value) : 3;

            var ks = GetLoginSecret();

            var channelDict = GetChannels(ks).ToDictionary(x => x.Id);            
            for (int i = 0; i <= daysForward; i++)
            {
                int pageIndex = 1;
                var d = DateTime.UtcNow.Date.AddDays(i);
                while (true)
                {
                    logger.WriteEntry($"Grabbing CellcomTV date {d:yyyy-MM-dd} (Page {pageIndex}) ...", LogType.Info);
                    var postData = new
                    {
                        apiVersion = ApiVersion,
                        clientTag = ClientTag,
                        filter = new
                        {
                            kSql = $"(and start_date>='{ToUnixTime(d)}' end_date<'{ToUnixTime(d.AddDays(1))}' asset_type='epg')",
                            orderBy = "START_DATE_ASC",
                            objectType = "KalturaSearchAssetFilter"
                        },
                        ks = ks,
                        pager = new
                        {
                            pageIndex = pageIndex,
                            pageSize = PageSize,
                        }
                    };
                    var response = ((JObject)JsonConvert.DeserializeObject(GetJSON($"{URLBASE}asset/action/list", JsonConvert.SerializeObject(postData))));

                    var res = response["result"];
                    if (res["objects"] != null)
                    {
                        var epgEntires = ((JArray)res["objects"]).ToList();
                        foreach (var epg in epgEntires)
                        {
                            if (channelDict.TryGetValue(epg.Value<int>("epgChannelId"), out var cd))
                            {
                                var s = new Show();
                                try
                                {                                    
                                    s.Channel = cd.Name;
                                    s.Title = epg.Value<string>("name");
                                    s.Description = epg.Value<string>("description");
                                    s.StartTime = FromUnixTime(epg.Value<long>("startDate"));
                                    s.EndTime = FromUnixTime(epg.Value<long>("endDate"));
                                    shows.Add(s);
                                }
                                catch (Exception ex)
                                {
                                    logger.LogException(ex,$"show  {s.Title} , channel {s.Channel}");
                                }
                            }

                        }
                        if (epgEntires.Count >= PageSize)
                            pageIndex++;
                        else
                            break;
                    }
                    else
                        break;                    
                }

            }

            return CleanupSameTimeStartEndShows(shows);
        }

        private List<ChannelData> GetChannels(string ks)
        {
            var lst = new List<ChannelData>();
            var postData = new
            {
                apiVersion = ApiVersion,
                clientTag = ClientTag,
                filter = new
                {
                    idEqual = 353875,
                    kSql = $"(and customer_type_blacklist!='1'  deep_link_type!='netflix'   Is_adult!='1'  deep_link_type!='youtube' (and PPV_module!+'') deep_link_type!='amazon')",                    
                    objectType = "KalturaChannelFilter"
                },
                ks = ks,
                pager = new
                {
                    pageIndex = 1,
                    pageSize = PageSize,
                }
            };
            var json = JsonConvert.SerializeObject(postData);
            var res = ((JObject)JsonConvert.DeserializeObject(GetJSON($"{URLBASE}asset/action/list", json)))["result"];
            foreach (var c in res["objects"])
            {
                lst.Add(new ChannelData { Id = c.Value<int>("externalIds"), Name = c.Value<string>("name") });
            }
            return lst;
        }

        string GetJSON(string url, string postData)
        {            
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";            
            var data = Encoding.ASCII.GetBytes(postData);
            req.ContentType = "application/json";            
            req.ContentLength = data.Length;
            using (var stream = req.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            req.UserAgent = Useragent;

            var res = (HttpWebResponse)req.GetResponse();
            using (var sr = new StreamReader(res.GetResponseStream()))
            {
                var startDownloadTime = DateTime.Now;
                var sb = new StringBuilder();
                int blockSize = 65536;
                while (!sr.EndOfStream)
                {
                    var buf = new char[blockSize];
                    var totalRead = sr.ReadBlock(buf, 0, blockSize);
                    sb.Append(buf);
                    if (DateTime.Now - startDownloadTime > TimeSpan.FromSeconds(1))
                    {
                        startDownloadTime = DateTime.Now;
                        _logger.WriteEntry(string.Format("Downloaded {0:#,##0} bytes so far", sb.Length), LogType.Info);
                    }
                }
                return sb.ToString();
            }
        }

        private string GetLoginSecret()
        {
            var postData = new
            {
                partnerId = PartnerId,
                udid = Guid.NewGuid().ToString("D")
            };
            var json = JsonConvert.SerializeObject(postData);
            var res = (JObject)JsonConvert.DeserializeObject(GetJSON($"{URLBASE}OTTUser/action/anonymousLogin", json));
            return res["result"].Value<string>("ks");
        }
    }
}
