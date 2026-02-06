using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SqlEssentials.Core.Logging
{
    public static class LogEntrySerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        static LogEntrySerializer()
        {
            Settings.Converters.Add(new StringEnumConverter());
        }

        public static string Serialize(LogEntry entry)
        {
            return JsonConvert.SerializeObject(entry, Settings);
        }
    }
}
