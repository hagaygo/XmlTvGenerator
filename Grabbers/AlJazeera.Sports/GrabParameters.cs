using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlTvGenerator.Core;

namespace AlJazeera.Sports
{
    public enum ChannelEnum
    {
        JscSportNews = 4404,
        JscSport1 = 5301,
        JscSport2 = 5302,
        JscSportGlobal = 5304,
        JscSportPlus1 = 2701,
        JscSportPlus2 = 2702,
        JscSportPlus3 = 2703,
        JscSportPlus4 = 2704,
        JscSportPlus5 = 2705,
        JscSportPlus6 = 801,
        JscSportPlus7 = 802,
        JscSportPlus8 = 803,
        JscSportPlus9 = 804,
        JscSportPlus10 = 805,
        JscSportHD1 = 5201,
        JscSportHD2 = 5202,
        JscSportHD3 = 5203,
        JscSportHD4 = 5204,
        JscSportHD5 = 4401,
        JscSportHD6 = 4402,
    }

    public class GrabParameters : GrabParametersBase
    {        
        public ChannelEnum ChannelId { get; set; }
    }
}
