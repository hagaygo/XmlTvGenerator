using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlTvGenerator.Core;

namespace Israel.Rashut2.Org.il
{
    public enum Channel
    {
        Channel2,
        Channel10,
    }

    public class GrabParameters : GrabParametersBase
    {
        public Channel Channel { get; set; }
        public DateTime Date { get; set; }
    }
}
