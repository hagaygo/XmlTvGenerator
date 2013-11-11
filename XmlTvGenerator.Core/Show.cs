using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlTvGenerator.Core
{
    public class Show
    {
        public Show Clone()
        {
            return (Show)MemberwiseClone();
        }

        public string Title { get; set; }
        public string Description { get; set; }
        public string Channel { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration
        {
            get { return EndTime - StartTime; }
        }

        public int? Series { get; set; }
        public int? Episode { get; set; }
    }
}
