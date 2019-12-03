using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XmlTvGenerator.Core;

namespace CyfrowyPolsat.pl
{
    public class GrabParameters : GrabParametersBase
    {
        public static List<string> AvailableChannels = new List<string>
    {
        // case sensitive !!!
        "rt-doc-hd",
        "rt-hd",
        "sky-news",
        "boomerang",
        "cnn",
        "cnbc",        
        "france-24-en-hd",
        "ci-polsat",
        "disney-junior",
        "cbs-europa",
        "cbs-reality",
        "cbs-action",
        "travel-channel",
        "arte-hd",
        "tve",
        "al-jazeera-hd",
        "polsat-sport-extra-hd",
        "polsat-sport-hd",
        "tvp-sport",
        "jim-jam-polsat",
        "bbc-cbeebies",
        "nsport-hd",
        "canal-plus-family-hd",
        "canal-plus-sport-hd",
        "discovery-life",
        "euronews-hd",
        "press-tv",
        "bloomberg_(ang.)",
    };
        public string Channel { get; set; }
    }
}