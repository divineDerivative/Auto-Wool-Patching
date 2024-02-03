using Verse;

namespace AutoWool
{
    internal static class Logging
    {
        private static bool DebugLogging => AutoWoolSettings.debugLogging;
        private static string WrapMessage(string message) => $"<color=#00b7dc>[AutoWool]</color> {message}";
        internal static void Message(string message, bool debugOnly = false)
        {
            if ((debugOnly && DebugLogging) || !debugOnly)
            {
                Log.Message(WrapMessage(message));
            }
        }

        internal static void Warning(string message, bool debugOnly = false)
        {
            if ((debugOnly && DebugLogging) || !debugOnly)
            {
                Log.Warning(WrapMessage(message));
            }
        }

        internal static void WarningOnce(string message, int key, bool debugOnly = false)
        {
            if ((debugOnly && DebugLogging) || !debugOnly)
            {
                Log.WarningOnce(WrapMessage(message), key);
            }
        }

        internal static void Error(string message, bool debugOnly = false)
        {
            if ((debugOnly && DebugLogging) || !debugOnly)
            {
                Log.Error(WrapMessage(message));
            }
        }

        internal static void ErrorOnce(string message, int key, bool debugOnly = false)
        {
            if ((debugOnly && DebugLogging) || !debugOnly)
            {
                Log.ErrorOnce(WrapMessage(message), key);
            }
        }
    }
}
