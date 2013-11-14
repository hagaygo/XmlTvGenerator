using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

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

        static Config _config;

        public static Config GetInstance()
        {
            if (_config == null)
            {
                _config = new Config();

                var configPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.xml";
                if (File.Exists(configPath))
                {
                    var doc = XDocument.Load(configPath);
                    foreach (var grabber in doc.Descendants("Grabber"))
                    {
                        var g = new GrabberSettings();
                        g.Path = grabber.Attribute("path").Value;
                        if (string.IsNullOrEmpty(g.Path))
                            continue; // no path , nothing to do here
                        g.ChannelPrefix = grabber.Attribute("channelPrefix").Value;
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
                }
            }
            return _config;
        }
    }

    public class GrabberSettings
    {
        public string Path { get; set; }
        public string Parameters { get; set; }
        public string ChannelPrefix { get; set; }
    }
}
