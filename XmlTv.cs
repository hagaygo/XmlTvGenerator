using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace XmlTvGenerator
{
    public class XmlTv
    {
        const string DateTimeFormat = "yyyyMMddHHmmss";

        public void Save(List<Show> shows,Stream output)
        {            
            var xml = new XDocument();
            var tv = new XElement("tv");
            xml.Add(tv);
            foreach (var channel in shows.Select(x => x.Channel).Distinct())
            {
                var c = new XElement("channel");
                c.Add(new XAttribute("id", channel));
                var dn = new XElement("display-name");
                c.Add(dn);
                dn.Value = channel;
                tv.Add(c);
            }
            foreach (var show in shows)
            {
                var programme = new XElement("programme");
                programme.Add(new XAttribute("start", show.StartTime.ToString(DateTimeFormat)));
                programme.Add(new XAttribute("stop", show.EndTime.ToString(DateTimeFormat)));
                programme.Add(new XAttribute("channel", show.Channel));
                if (!string.IsNullOrEmpty(show.Description))
                {
                    programme.Add(new XElement("title") { Value = show.Title });
                    programme.Add(new XElement("desc") { Value = show.Description });
                }
                else
                    if (!string.IsNullOrEmpty(show.Title))
                        programme.Add(new XElement("title") { Value = show.Title });
                    else
                        programme.Add(new XElement("title") { Value = "N/A" });
                if (show.Episode.HasValue)
                {
                    var epNum = new XElement("episode-num") { Value = show.Episode.Value.ToString() };
                    epNum.Add(new XAttribute("system", "onscreen"));
                    programme.Add(epNum);
                }
                tv.Add(programme);
            }
            xml.Save(output);
        }
    }
}
