using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XmlTvGenerator.Core.Translator.Cache
{
    /// <summary>
    /// does nothing
    /// </summary>
    public class DummyCacheManager : CacheManagerBase
    {
        public override void SaveCache()
        {
            
        }

        public override void Add(Language from, Language to, string sourceText, string translatedText)
        {
            
        }

        public override string Get(Language from, Language to, string text)
        {
            return null;
        }
    }
}
