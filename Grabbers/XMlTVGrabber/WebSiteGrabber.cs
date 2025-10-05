using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace XMlTVGrabber
{
    public class WebSiteGrabber : GrabberBase
    {
        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {            
            string format = "yyyyMMddHHmmss zzz";


            var doc = XDocument.Parse(xmlParameters);            
            var selectedChannels = doc.Descendants("Channels").Elements().Select(x => x.Value).ToList();
            var channelHash = selectedChannels.ToDictionary(x => x);
            var urls = doc.Descendants("Sources").Elements().Select(x => x.Value).ToList();
            var shows = new List<Show>();
            foreach (var url in urls) 
            {
                var wr = (HttpWebRequest)WebRequest.Create(url);
                logger.WriteEntry($"XMLTV grabber fetching {url} for {selectedChannels.Count} channels", LogType.Info);
                using (var res = (HttpWebResponse)wr.GetResponse())
                using (var sr = new StreamReader(res.GetResponseStream()))
                {
                    var xmltv = sr.ReadToEnd();
                    var xmlDoc = XDocument.Parse(xmltv);
                    foreach (var el in xmlDoc.Descendants("programme"))
                    {
                        if (channelHash.ContainsKey(el.Attribute("channel").Value))
                        {
                            try
                            {
                                var s = new Show();
                                s.Channel = el.Attribute("channel").Value;
                                s.Title = el.Descendants("title").First().Value;
                                s.Description = el.Descendants("desc").FirstOrDefault()?.Value;
                                s.StartTime = DateTime.ParseExact(el.Attribute("start").Value, format, CultureInfo.InvariantCulture).ToUniversalTime();
                                s.EndTime = DateTime.ParseExact(el.Attribute("stop").Value, format, CultureInfo.InvariantCulture).ToUniversalTime();
                                shows.Add(s);
                            }
                            catch (Exception ex)
                            {
                                logger.WriteEntry($"XMLTV Grabber error {ex.Message}", LogType.Error);  
                            }
                        }
                    }
                }                
            }
            

            return shows;
        }
    }
}
