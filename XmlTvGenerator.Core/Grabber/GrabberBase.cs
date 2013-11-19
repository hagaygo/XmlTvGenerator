using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XmlTvGenerator.Core
{
    public abstract class GrabberBase
    {
        public abstract List<Show> Grab(string xmlParameters, ILogger logger);

        protected void FixShowsEndTimeByStartTime(List<Show> shows)
        {
            for (int i = shows.Count - 2; i >= 0; i--)
            {
                shows[i].EndTime = shows[i + 1].StartTime;
            }
        }
    }
}
