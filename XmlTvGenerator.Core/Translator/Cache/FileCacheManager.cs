﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XmlTvGenerator.Core.Translator.Cache
{
    public class FileCacheManager : CacheManagerBase
    {
        string CachePath;

        public FileCacheManager(string filename)
        {
            if (Path.GetFileName(filename) == filename)
                CachePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar , filename);
            else
                CachePath = filename;
            if (File.Exists(CachePath))
            {
                LoadCache();
            }
        }

        public override void SaveCache()
        {
            var xml = new XDocument();
            var items = new XElement("items");
            xml.Add(items);
            foreach (var key in _cache.Keys)
            {
                var item = new XElement("item", _cache[key]);
                item.Add(new XAttribute("key", key));
                items.Add(item);
            }
            using (var wr = System.Xml.XmlWriter.Create(CachePath, new System.Xml.XmlWriterSettings() { CheckCharacters = false }))
            {
                xml.Save(wr);
            }
        }

        private void LoadCache()
        {
            using (var xr = System.Xml.XmlReader.Create(CachePath, new System.Xml.XmlReaderSettings() { CheckCharacters = false }))
            {
                var xml = XDocument.Load(xr);
                var items = xml.Element("items");
                _cache = new Dictionary<string, string>();
                foreach (var item in items.Elements("item"))
                {
                    _cache[item.Attribute("key").Value] = item.Value;
                }
            }
        }

        Dictionary<string, string> _cache = new Dictionary<string, string>();

        public override void Add(Language from, Language to, string sourceText, string translatedText)
        {
            _cache[from.ToString() + "|" + to.ToString() + "|" + sourceText] = translatedText;
        }

        public override string Get(Language from, Language to, string text)
        {
            string res;
            if (_cache.TryGetValue(from.ToString() + "|" + to.ToString() + "|" + text, out res))
            {
                return res;
            }
            return null;
        }
    }
}
