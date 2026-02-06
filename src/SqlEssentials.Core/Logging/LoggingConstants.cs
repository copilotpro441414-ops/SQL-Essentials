namespace SqlEssentials.Core.Logging
{
    public static class LoggingConstants
    {
        public const string LogLevelEnvVar = "SQL_ESSENTIALS_LOG_LEVEL";
        public const string LogMaxMbEnvVar = "SQL_ESSENTIALS_LOG_MAX_MB";
        public const string LogFilePrefix = "SqlEssentials";
        public const string LogFileExtension = ".debug.log";
        public const int DefaultMaxFileSizeMb = 10;
        public const int MaxQueueSize = 1000;
    }
}
