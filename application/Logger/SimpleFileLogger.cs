using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace application.Logger
{
    public class SimpleFileLogger : ISyncLogger
    {
        private string _fileName;
        public LoggingLevel Level { get; set; }

        public SimpleFileLogger()
        {
            if (!Directory.Exists(@"Log"))
                Directory.CreateDirectory(@"Log");
            
            _fileName = $"Log/Log_{DateTime.Now.ToString().Replace("/", "-").Replace(":", "-").Replace(" ", "_")}.log";
            
        }

        public void WriteEntry(string entry, LoggingLevel level = LoggingLevel.Error, int updateId = -1)
        {
            if (Level <= level)
            {
                if(updateId > 0)//to drop each update in a single file
                    File.AppendAllLines(_fileName + updateId, new List<string> { $"{DateTime.Now.ToString("s")}|{level.ToString().ToUpper()}|{entry}" });
                else
                    File.AppendAllLines(_fileName, new List<string> { $"{DateTime.Now.ToString("s")}|{level.ToString().ToUpper()}|{entry}" });

                Console.WriteLine($"{DateTime.Now.ToString("s")}|{level.ToString().ToUpper()}|{entry}");
            }
        }

    }
}
