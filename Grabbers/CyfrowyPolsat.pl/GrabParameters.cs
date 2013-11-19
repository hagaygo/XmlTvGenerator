using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XmlTvGenerator.Core;

namespace CyfrowyPolsat.pl
{
    public enum Channel
    {
        boomerang,
        ci_polsat,
        disney_junior,
        cbs_drama,
        cbs_europa,
        cbs_reality,
        tcm,
        travel_channel,
        arte_hd,
        tve,
        russia_today,
        bbc_hd
    }

    public class GrabParameters : GrabParametersBase
    {
        public Channel Channel { get; set; }        
    }
}
