using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlTvGenerator.Core;

namespace XmlTvGenerator.Logger
{
    /// <summary>
    /// does nothing
    /// </summary>
    public class DummyLogger : ILogger
    {
        public void InitLog(string xmlSettings)
        {
            
        }

        public void WriteEntry(string text, LogType lt)
        {
            
        }
    }
}
