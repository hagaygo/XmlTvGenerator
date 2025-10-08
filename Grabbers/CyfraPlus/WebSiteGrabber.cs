using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace CyfraPlus
{
    public class WebSiteGrabber : GrabberBase
    {           

        public override List<Show> Grab(string xmlParameters, ILogger logger)
        {
            logger.WriteEntry("Cyfra+ grabber started...", LogType.Info);
            var shows = new List<Show>();            
            FillGlobalShowsData(shows);
            logger.WriteEntry("Cyfra+ grabber finished.", LogType.Info);
            return shows;
        }

        private void FillGlobalShowsData(List<Show> shows)
        {            
            var wr = WebRequest.CreateHttp("https://secure-webtv-static.canal-plus.com/metadata/cppol/all/v2.2/globalchannels.json"); // holds only current day data few hours forward
            using (var res = (HttpWebResponse)wr.GetResponse())
            {
                var channels = JObject.Parse(new StreamReader(res.GetResponseStream()).ReadToEnd())["channels"];
                foreach (var chan in channels)
                {
                    foreach (var ev in chan["events"])
                    {
                        var show = new Show();
                        show.Channel = chan.Value<string>("name");                        
                        show.Title = ev.Value<string>("title");
                        show.Description = ev.Value<string>("subTitle");
                        show.StartTime = ev.Value<DateTime>("startDate");
                        show.EndTime = ev.Value<DateTime>("endDate");
                        shows.Add(show);
                    }
                }
            }
        }        
    }    
}
