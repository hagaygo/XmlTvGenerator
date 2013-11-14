using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlTvGenerator.Core;

namespace BBC.World.News
{
    public class GrabParameters : GrabParametersBase
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}
