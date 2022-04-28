using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XmlTvGenerator.Core
{
    public enum LogType
    {
        Info,
        Warning,
        Error,
    }

    public interface ILogger
    {
        void InitLog(string xmlSettings);
        void WriteEntry(string text, LogType lt);
        void LogException(Exception ex, string errorPrefix = null);
    }
}
