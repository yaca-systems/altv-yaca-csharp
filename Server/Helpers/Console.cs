using AltV.Net;
using AltV.Net.ColoredConsole;

namespace Server.Helpers
{
    internal class Console
    {
        public static void Error(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Alt.LogColored(new ColoredMessage() + TextColor.Red + "[YaCA] " + message);
            }
        }

        public static void Log(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Alt.LogColored(new ColoredMessage() + TextColor.Green + "[YaCA] " + message);
            }
        }

        public static void Information(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Alt.LogColored(new ColoredMessage() + TextColor.Blue + "[YaCA] " + message);
            }
        }
    }
}
