using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XmlTvGenerator.Core.Translator;

namespace XmlTvGenerator
{
    public class Config
    {
        protected Config()
        {
            Grabbers = new List<GrabberSettings>();
        }

        public List<GrabberSettings> Grabbers { get; set; }
        public string OutputPath { get; set; }
        public string LoggerType { get; set; }
        public string LoggerParameters { get; set; }
        public TranslatorCacheSettings TranslatorCacheSettings { get; set; }
        public bool HaltOnGrabberError { get; set; }

        static Config _config;

        public static Config GetInstance()
        {
            if (_config == null)
            {
                _config = new Config();

                var configPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar, "config.xml");
                if (File.Exists(configPath))
                {
                    var doc = XDocument.Load(configPath);
                    foreach (var grabber in doc.Descendants("Grabber"))
                    {
                        var g = new GrabberSettings();
                        g.Path = grabber.Attribute("path").Value;
                        if (string.IsNullOrEmpty(g.Path))
                            continue; // no path , nothing to do here
                        g.ChannelPrefix = grabber.Attribute("channelPrefix") != null ? grabber.Attribute("channelPrefix").Value : string.Empty;
                        var translationElement = grabber.Descendants("Translate").FirstOrDefault();
                        if (translationElement != null)
                        {
                            g.Translation = new GrabberTranslationSettings();
                            g.Translation.From = (Language)Enum.Parse(typeof(Language), translationElement.Attribute("From").Value);
                            g.Translation.To = (Language)Enum.Parse(typeof(Language), translationElement.Attribute("To").Value);
                            if (translationElement.Attribute("MaxDegreeOfParallelism") != null)
                                g.Translation.MaxDegreeOfParallelism = Convert.ToInt32(translationElement.Attribute("MaxDegreeOfParallelism").Value);
                            if (translationElement.Attribute("YandexAPIKeysFilePath") != null)
                                g.Translation.YandexAPIKeysFilePath = translationElement.Attribute("YandexAPIKeysFilePath").Value;
                        }
                        var paramElement = grabber.Descendants("Parameters").FirstOrDefault();
                        if (paramElement != null)
                            g.Parameters = paramElement.ToString();
                        _config.Grabbers.Add(g);
                    }
                    var outputElement = doc.Descendants("Output").FirstOrDefault();
                    if (outputElement != null)
                    {
                        var pathElement = outputElement.Descendants("Path").FirstOrDefault();
                        if (pathElement != null)
                            _config.OutputPath = pathElement.Value;
                    }
                    var loggerElement = doc.Descendants("Logger").FirstOrDefault();
                    if (loggerElement != null && loggerElement.Attribute("Type") != null)
                    {
                        _config.LoggerType = loggerElement.Attribute("Type").Value;
                        var loggerParameters = loggerElement.Descendants("Parameters").FirstOrDefault();
                        if (loggerParameters != null)
                            _config.LoggerParameters = loggerParameters.ToString();
                    }
                    var translatorCache = doc.Descendants("TranslatorCache").FirstOrDefault();
                    if (translatorCache != null && translatorCache.Attribute("Type") != null)
                    {
                        _config.TranslatorCacheSettings = new TranslatorCacheSettings();
                        _config.TranslatorCacheSettings.Type = TranslatorCacheSettingsType.File;
                        _config.TranslatorCacheSettings.OutputFile = translatorCache.Attribute("OutputFile").Value;
                    }
                    var HaltOnGrabberErrorElement = doc.Descendants("HaltOnGrabberError").FirstOrDefault();
                    if (HaltOnGrabberErrorElement != null && !string.IsNullOrEmpty(HaltOnGrabberErrorElement.Value))
                        _config.HaltOnGrabberError = Convert.ToBoolean(HaltOnGrabberErrorElement.Value);
                }
                else
                    Console.WriteLine("config file not found");
            }
            return _config;
        }
    }

    public class GrabberSettings
    {
        public string Path { get; set; }
        public string Parameters { get; set; }
        public string ChannelPrefix { get; set; }
        public GrabberTranslationSettings Translation { get; set; }
    }

    public class GrabberTranslationSettings
    {
        public Language From { get; set; }
        public Language To { get; set;  }
        public string YandexAPIKeysFilePath { get; set; }
        public int MaxDegreeOfParallelism { get; set; } = 1;
    }
}
