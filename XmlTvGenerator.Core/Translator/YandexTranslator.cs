using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using XmlTvGenerator.Core.Translator.Cache;

namespace XmlTvGenerator.Core.Translator
{
    public class YandexTranslator : TranslatorBase
    {
        static Random rnd = new Random();
        public override string Translate(Language from, Language to, string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            var cacheText = Cache == null ? null : Cache.Get(from, to, text);
            if (cacheText != null)
                return cacheText;            

            var web = (HttpWebRequest)WebRequest.Create(string.Format("https://translate.yandex.net/api/v1.5/tr.json/translate?key={3}&text={0}&lang={1}-{2}", HttpUtility.UrlEncode(text), _languageDict[from], _languageDict[to], _apiKeys[rnd.Next(_apiKeys.Count)] ));
            var res = (HttpWebResponse)web.GetResponse();
            using (var sr = new StreamReader(res.GetResponseStream()))
            {
                var resultText = sr.ReadToEnd();
                if (resultText.Length > 2)
                {
                    var obj = Newtonsoft.Json.Linq.JObject.Parse(resultText);
                    var output = obj["text"].ToString();
                    if (output.StartsWith("["))
                        output = output.Substring(1, output.Length - 2).Trim();
                    if (from == Language.Polish)
                        output = output.Replace("UNP.", "Episode ");
                    if (output.StartsWith("\"") && output.EndsWith("\""))
                        output = output.Substring(1, output.Length - 2).Trim();
                    if (output != null && Cache != null)
                        Cache.Add(from, to, text, output);
                }
            }

            return null;

            throw new NotImplementedException();
        }

        Dictionary<Language, string> _languageDict;

        List<string> _apiKeys;

        public YandexTranslator(CacheManagerBase cache, int maxDegreeOfParallelism, string apiKeysFilePath)
            : base(cache, maxDegreeOfParallelism)
        {
            _languageDict = GetYandexLanguageDict();
            _apiKeys = File.ReadAllLines(apiKeysFilePath).ToList();
        }

        Dictionary<Language, string> GetYandexLanguageDict()
        {
            var dict = new Dictionary<Language, string>();
            dict[Language.Esperanto] = "eo";
            dict[Language.English] = "en";
            dict[Language.Dutch] = "nl";
            dict[Language.Danish] = "da";
            dict[Language.Czech] = "cs";
            dict[Language.Croatian] = "hr";
            dict[Language.Chinese] = "zh-CN";
            dict[Language.Catalan] = "ca";
            dict[Language.Bulgarian] = "bg";
            dict[Language.Bengali] = "bn";
            dict[Language.Belarusian] = "be";
            dict[Language.Basque] = "eu";
            dict[Language.Azerbaijani] = "az";
            dict[Language.Armenian] = "hy";
            dict[Language.Arabic] = "ar";
            dict[Language.Albanian] = "sq";
            dict[Language.Indonesian] = "id";
            dict[Language.Icelandic] = "is";
            dict[Language.Hungarian] = "hu";
            dict[Language.Hindi] = "hi";
            dict[Language.Hebrew] = "iw";
            dict[Language.Haitian_Creole] = "ht";
            dict[Language.Greek] = "el";
            dict[Language.Georgian] = "ka";
            dict[Language.German] = "de";
            dict[Language.Galician] = "gl";
            dict[Language.French] = "fr";
            dict[Language.Finnish] = "fi";
            dict[Language.Filipino] = "tl";
            dict[Language.Estonian] = "et";
            dict[Language.Macedonian] = "mk";
            dict[Language.Lithuanian] = "lt";
            dict[Language.Latvian] = "lv";
            dict[Language.Latin] = "la";
            dict[Language.Lao] = "lo";
            dict[Language.Korean] = "ko";
            dict[Language.Japanese] = "ja";
            dict[Language.Italian] = "it";
            dict[Language.Irish] = "ga";
            dict[Language.Slovenian] = "sl";
            dict[Language.Slovak] = "sk";
            dict[Language.Serbian] = "sr";
            dict[Language.Russian] = "ru";
            dict[Language.Romanian] = "ro";
            dict[Language.Portuguese] = "pt";
            dict[Language.Polish] = "pl";
            dict[Language.Persian] = "fa";
            dict[Language.Norwegian] = "no";
            dict[Language.Maltese] = "mt";
            dict[Language.Malay] = "ms";
            dict[Language.Spanish] = "es";
            dict[Language.Swahili] = "sw";
            dict[Language.Swedish] = "sv";
            dict[Language.Tamil] = "ta";
            dict[Language.Telugu] = "te";
            dict[Language.Thai] = "th";
            dict[Language.Turkish] = "tr";
            dict[Language.Ukrainian] = "uk";
            dict[Language.Urdu] = "ur";
            dict[Language.Vietnamese] = "vi";
            dict[Language.Welsh] = "cy";
            dict[Language.Yiddish] = "yi";
            return dict;
        }
    }
}
