using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XmlTvGenerator.Core.Translator.Cache
{
    public abstract class CacheManagerBase
    {
        public abstract void SaveCache();
        public abstract void Add(Language from, Language to, string sourceText, string translatedText);
        public abstract string Get(Language from, Language to, string text);
    }
}
