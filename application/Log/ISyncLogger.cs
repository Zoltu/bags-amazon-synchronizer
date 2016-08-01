namespace application.Log
{
    public enum LoggingLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3
    }

    public interface ISyncLogger
    {
        LoggingLevel Level { get; set; }
        void WriteEntry(string entry, LoggingLevel level = LoggingLevel.Error);
    }
}
