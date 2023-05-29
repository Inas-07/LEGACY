using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.IO;
using ScanPosOverride.JSON;

namespace LEGACY.Utils
{
    internal static class Json
    {
        private static readonly JsonSerializerOptions _setting = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            IncludeFields = false,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            IgnoreReadOnlyProperties = true
        };


        static Json()
        {
            _setting.Converters.Add(new JsonStringEnumConverter());
            // from ScanPositionOverride
            _setting.Converters.Add(MTFOPartialDataUtil.PersistentIDConverter);
            _setting.Converters.Add(MTFOPartialDataUtil.LocalizedTextConverter);

            // if not using partial data, use this line instead
            //_setting.Converters.Add(new LocalizedTextConverter());
        }

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _setting);
        }

        public static object Deserialize(Type type, string json)
        {
            return JsonSerializer.Deserialize(json, type, _setting);
        }

        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, _setting);
        }

        public static void Load<T>(string filePath, out T config) where T : new()
        {
            config = Deserialize<T>(File.ReadAllText(filePath));
        }
    }
}
