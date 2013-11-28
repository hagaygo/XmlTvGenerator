using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlTvGenerator.Core;

namespace Sky.it
{
    public enum Channel
    {
        sky_supercalcio_hd = 200,
        sky_calcio_1_hd = 201,
        sky_calcio_2_hd = 202,
        sky_calcio_3_hd = 203,
        sky_calcio_4_hd = 204,
        sky_calcio_5_hd = 205,
        sky_calcio_6_hd = 206,
        sky_calcio_7_hd = 207,
        sky_calcio_8_hd = 208,
        sky_calcio_9 = 209,
        sky_calcio_10 = 210,
        sky_calcio_11 = 211,
        sky_calcio_12 = 212,
        sky_calcio_13 = 213,
        sky_sport_1_hd = 101,
        sky_sport_2_hd = 102,
        sky_sport_3_hd = 103,
        fox_sports_hd = 301,
        fox_sports_plus_hd = 302,
        supertennis_hd = 401,

        comedy_central = 500,
        real_time_hd = 501,
        axn_sci_fi = 502,
        gxt = 503,
        sky_cinema_1_hd = 504,
        sky_cinema_007_hd = 505,
        sky_cinema_family_hd = 506,
        sky_cinema_passion_hd = 507,
        sky_cinema_max_hd = 508,
        sky_cinema_cult_hd = 509,
        sky_cinema_classics_hd = 510,
        discovery_channel_hd = 511,
        national_geo_hd = 512,
        discovery_science_hd = 513,
        discovery_travel_AANNDD_living_hd = 514,
        history_channel_hd = 515,
        nat_geo_wild_hd = 516,
        nat_geo_adventure_hd = 517,
        dove_tv = 518,
        sky_cinema_comedy_hd = 519,
        fox_news = 520,
        cnn_intl = 521,
        fox_business = 522,
        mtv_hits = 523,
        mtv_classic = 524,
        mtv_music = 525,
        mtv_dance = 526,
        nick_junior = 527,
        disney_junior = 528,
        cartoon_network = 529,
        boomerang = 530,
        sky_sport_f1_hd = 531,
        sportitalia = 532,
        sportitalia_2 = 533,
        sky_news = 534,
        france_24_english__ = 535,
        animal_planet = 536,
        fox_crime_hd = 537,
        axn_hd = 538,
        sky_uno_hd = 539,
        fox_hd = 540,
        fox_life_hd = 541,
        cnbc = 542,
        russia_today = 543,
        bloomberg_ = 544,
        sky_cinema_PPLLUUSS_24_hd = 545,
    }

    public class GrabParameters : GrabParametersBase
    {
        public Channel Channel { get; set; }
    }
}
