using System.Diagnostics;
using System.IO;
using SqlEssentials.Core.Logging;

namespace SqlEssentials.Extension.Logging
{
    public static class LogPathBuilder
    {
        public static string GetLogFilePath()
        {
            var tempPath = Path.GetTempPath();
            var pid = Process.GetCurrentProcess().Id;
            var fileName = $"{LoggingConstants.LogFilePrefix}.{pid}{LoggingConstants.LogFileExtension}";
            return Path.Combine(tempPath, fileName);
        }

        public static string GetBackupFilePath(string logFilePath)
        {
            return logFilePath + ".1";
        }
    }
}
