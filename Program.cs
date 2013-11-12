using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XmlTvGenerator.Core;

namespace XmlTvGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Show> shows = GetShows();
            var xmltv = new XmlTv();
            using (var f = new FileStream(@"e:\temp\t.xml", FileMode.OpenOrCreate))
                xmltv.Save(shows, f);
        }

        private static List<Show> GetShows()
        {
            var lst = new List<Show>();
            
            return lst;
        }        
    }
}
