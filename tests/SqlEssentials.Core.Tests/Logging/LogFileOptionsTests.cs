using SqlEssentials.Core.Logging;
using Xunit;

namespace SqlEssentials.Core.Tests.Logging
{
    public sealed class LogFileOptionsTests
    {
        [Fact]
        public void MaxFileSizeBytes_ComputesFromMegabytes()
        {
            var options = new LogFileOptions
            {
                LogFilePath = @"C:\\temp\\log.txt",
                MaxFileSizeMb = 10
            };

            Assert.Equal(@"C:\\temp\\log.txt", options.LogFilePath);
            Assert.Equal(10 * 1024 * 1024, options.MaxFileSizeBytes);
        }

        [Fact]
        public void MaxFileSizeBytes_IsZero_WhenMegabytesZero()
        {
            var options = new LogFileOptions { MaxFileSizeMb = 0 };

            Assert.Equal(0, options.MaxFileSizeBytes);
        }
    }
}
