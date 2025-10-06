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

            var shows = new List<Show>();

            var doc = XDocument.Parse(xmlParameters);
            var sources = doc.Descendants("Sources").Elements();
            foreach (var source in sources)
            {
                var selectedChannels = source.Descendants("Channels").Elements().Select(x => x.Value).ToList();
                var channelHash = selectedChannels.ToDictionary(x => x);
                bool useSubTitleAsTitle = source.Attribute("UseSubTitleAsTitle")?.Value == "true";
                var url = source.Descendants("Url").First().Value;

                var wr = (HttpWebRequest)WebRequest.Create(url);
                logger.WriteEntry($"XMLTV grabber fetching {url} for {selectedChannels.Count} channels", LogType.Info);
                using (var res = (HttpWebResponse)wr.GetResponse())
                using (var sr = new StreamReader(res.GetResponseStream()))
                {
                    var sourceShows = new List<Show>();
                    var xmltv = sr.ReadToEnd();
                    logger.WriteEntry($"XMLTV grabber {url} Downloaded, parsing XMLTV...", LogType.Info);
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
                                s.SubTitle = el.Descendants("sub-title").FirstOrDefault()?.Value;
                                s.Description = el.Descendants("desc").FirstOrDefault()?.Value;
                                if (useSubTitleAsTitle && !string.IsNullOrEmpty(s.SubTitle))
                                {
                                    var title = s.Title;
                                    s.Title = s.SubTitle;
                                    s.SubTitle = title;                                    
                                }
                                if (string.IsNullOrEmpty(s.Description))
                                {
                                    s.Description = s.SubTitle;
                                    s.SubTitle = null;
                                }
                                s.StartTime = DateTime.ParseExact(el.Attribute("start").Value, format, CultureInfo.InvariantCulture).ToUniversalTime();
                                s.EndTime = DateTime.ParseExact(el.Attribute("stop").Value, format, CultureInfo.InvariantCulture).ToUniversalTime();
                                sourceShows.Add(s);
                            }
                            catch (Exception ex)
                            {
                                logger.WriteEntry($"XMLTV Grabber error {ex.Message}", LogType.Error);
                            }
                        }
                    }
                    logger.WriteEntry($"XMLTV grabber {url} Found {sourceShows.Count} EPG Entries", LogType.Info);
                    shows.AddRange(sourceShows);
                }                
            }            

            return shows;
        }
    }
}
