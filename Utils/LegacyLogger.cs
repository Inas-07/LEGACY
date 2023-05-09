using BepInEx.Logging;

namespace LEGACY.Utils
{
    internal static class LegacyLogger
    {
        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("LEGACYCore");

        public static void Log(string format, params object[] args)
        {
            LegacyLogger.Log(string.Format(format, args));
        }

        public static void Log(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Message, str);
        }

        public static void Warning(string format, params object[] args)
        {
            LegacyLogger.Warning(string.Format(format, args));
        }

        public static void Warning(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Warning, str);
        }

        public static void Error(string format, params object[] args)
        {
            LegacyLogger.Error(string.Format(format, args));
        }

        public static void Error(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Error, str);
        }

        public static void Debug(string format, params object[] args)
        {
            LegacyLogger.Debug(string.Format(format, args));
        }

        public static void Debug(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Debug, str);
        }
    }

}
