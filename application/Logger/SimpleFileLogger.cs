using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace application.Logger
{
    public class SimpleFileLogger : ISyncLogger
    {
        private StringBuilder _logBuilder;
        private string _fileName;
        public LoggingLevel Level { get; set; }

        public SimpleFileLogger()
        {
            _logBuilder = new StringBuilder();
            _fileName = $"Log_{DateTime.Now.ToString().Replace("/", "-").Replace(":", "-").Replace(" ", "_")}.log";
        }

        public void WriteEntry(string entry, LoggingLevel level = LoggingLevel.Error)
        {
            if (Level <= level)
            {
                File.AppendAllLines(_fileName, new List<string> { $"{DateTime.Now.ToString("s")}|{level.ToString().ToUpper()}|{entry}" });
                Console.WriteLine($"{DateTime.Now.ToString("s")}|{level.ToString().ToUpper()}|{entry}");
            }
        }

    }
}
