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
        cbs_europa,
        cbs_reality,
        cbs_action,
        travel_channel,
        arte_hd,
        tve,        
        bbc_hd,
        universal_channel,
        scifi_universal,        
        al_jazeera,
        polsat_sport_extra_hd,
        polsat_sport_hd,
        tvp_sport,
        jim_jam_polsat,
        bbc_cbeebies,
        nsport_hd,
        canal_plus_family_hd,
        canal_plus_sport_hd
    }

    public class GrabParameters : GrabParametersBase
    {
        public Channel Channel { get; set; }        
    }
}
