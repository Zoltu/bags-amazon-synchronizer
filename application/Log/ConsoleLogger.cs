﻿using System;

namespace application.Log
{
    public class ConsoleLogger : ISyncLogger
    {
        public LoggingLevel Level { get; set; } = LoggingLevel.Debug;

        public void WriteEntry(string entry, LoggingLevel level = LoggingLevel.Error)
        {
           
            if(Level <= level)
                Console.WriteLine($"{DateTime.Now.ToString("s")}|{level.ToString().ToUpper()}|{entry}");
        }
    }
}
