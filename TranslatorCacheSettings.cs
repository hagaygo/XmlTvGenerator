using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XmlTvGenerator
{
    public enum TranslatorCacheSettingsType
    {
        File,
    }

    public class TranslatorCacheSettings
    {
        public TranslatorCacheSettingsType Type { get; set; }
        public string OutputFile { get; set; }
    }
}
