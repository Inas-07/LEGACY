using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.IO;
using ExtraObjectiveSetup.JSON;

namespace LEGACY.Utils
{
    internal static class Json
    {

        static Json()
        {
        }

        public static T Deserialize<T>(string json) => EOSJson.Deserialize<T>(json);
        
        public static object Deserialize(Type type, string json) => EOSJson.Deserialize(type, json);

        public static string Serialize<T>(T value) => EOSJson.Serialize(value);

        public static void Load<T>(string filePath, out T config) where T : new()
        {
            config = Deserialize<T>(File.ReadAllText(filePath));
        }
    }
}
