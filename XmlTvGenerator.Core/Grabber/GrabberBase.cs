using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XmlTvGenerator.Core
{
    public abstract class GrabberBase
    {
        public DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public long ToUnixTime(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }

        public abstract List<Show> Grab(string xmlParameters, ILogger logger);

        protected void FixShowsEndTimeByStartTime(List<Show> shows)
        {
            if (shows.Count > 1)
            {
                for (int i = shows.Count - 2; i >= 0; i--)
                {
                    shows[i].EndTime = shows[i + 1].StartTime;
                }
                var s = shows[shows.Count - 1];
                if (s.StartTime > s.EndTime)
                    s.EndTime = s.StartTime;
            }
            if (shows.Count >= 1)
                if (shows[shows.Count -1].EndTime == shows[shows.Count - 1].StartTime)
                    shows[shows.Count -1].EndTime = shows[shows.Count-1].StartTime.AddHours(2);
        }
    }
}
