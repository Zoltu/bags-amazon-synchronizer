using System;

namespace application.Logger
{
    public class ConsoleLogger : ISyncLogger
    {
        public LoggingLevel Level { get; set; } = LoggingLevel.Debug;

        public void WriteEntry(string entry, LoggingLevel level = LoggingLevel.Error, int updateId = -1)
        {
           
            if(Level <= level)
                Console.WriteLine($"{DateTime.Now.ToString("s")}|{level.ToString().ToUpper()}|{entry}");
        }
    }
}
