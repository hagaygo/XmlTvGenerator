using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace XmlTvGenerator.Core
{
    public static class Tools
    {
        public static string CleanupText(string text)
        {
            var sb = new StringBuilder(text);
            sb.Replace("\t", string.Empty);
            sb.Replace("\r", string.Empty);
            sb.Replace("\n", string.Empty);
            sb.Replace("\\u0026", "&");
            return HttpUtility.HtmlDecode(sb.ToString().Trim()).Trim();

        }
    }
}
