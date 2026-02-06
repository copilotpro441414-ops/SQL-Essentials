using System;

namespace SqlEssentials.Extension.Commands
{
    internal static class CommandIds
    {
        public const string PackageGuidString = "8B8C2B19-7B1F-4B4F-9E8A-2F3B0E0B0E6C";
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);

        public static readonly Guid CommandSet = new Guid("1D42F4BA-8798-4C2F-9C6B-8A2E9C6B42F1");

        public const int RefreshSchema = 0x0100;
        public const int LogContentType = 0x0101;
    }
}
