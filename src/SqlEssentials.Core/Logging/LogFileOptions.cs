namespace SqlEssentials.Core.Logging
{
    public sealed class LogFileOptions
    {
        public string LogFilePath { get; set; }
        public int MaxFileSizeMb { get; set; }
        public int MaxFileSizeBytes => MaxFileSizeMb * 1024 * 1024;
    }
}
