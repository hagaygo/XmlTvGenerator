using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XmlTvGenerator.Core;

namespace XmlTvGenerator.Logger
{
    public class FileLogger : ILogger
    {
        StreamWriter sw;

        public void WriteEntry(string text, LogType lt)
        {
#if DEBUG
            Console.WriteLine(text);
#endif
            sw.WriteLine(string.Format("{0}\t{1}\t{2}", DateTime.Now.ToString(), lt.ToString(), text));
            sw.Flush();
        }

        public void InitLog(string xmlSettings)
        {
            var doc = XDocument.Parse(xmlSettings);
            var filename = doc.Descendants("Filename").FirstOrDefault();
            if (filename == null || string.IsNullOrEmpty(filename.Value))
                throw new ApplicationException("File logger no filename defined");
            var f = new FileStream(filename.Value, FileMode.OpenOrCreate);
            f.Seek(0, SeekOrigin.End);
            sw = new StreamWriter(f);
        }

        public void LogException(Exception ex, string errorPrefix = null)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            if (string.IsNullOrEmpty(errorPrefix))
                errorPrefix += " ";
            WriteEntry($"{errorPrefix}{ex.Message}\n{ex.StackTrace}", LogType.Error);
        }
    }
}
