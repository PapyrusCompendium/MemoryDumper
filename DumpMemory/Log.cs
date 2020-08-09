using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpMemory
{
    public static class Log
    {
        [Flags]
        public enum LogLevel
        {
            Debugging,
            Errors,
            Warnings,
            Info
        }

        public static LogLevel Level { get; set; }

        static Log()
        {
#if DEBUG
            Level = LogLevel.Info | LogLevel.Warnings | LogLevel.Errors | LogLevel.Debugging;
#else
                Level = LogLevel.Info | LogLevel.Warnings | LogLevel.Errors;
#endif
        }

        private static string _logName
        {
            get => $"{new StackTrace().GetFrames().Last().GetMethod().DeclaringType.Name}.cs";
        }

        private static string _timeStamp { get => DateTime.Now.ToLongTimeString(); }

        public static void Info(string info)
        {
            if (Level.HasFlag(LogLevel.Info))
                LogOutput(ConsoleColor.Green, info);
        }

        public static void Error(string error)
        {
            if (Level.HasFlag(LogLevel.Errors))
                LogOutput(ConsoleColor.Red, error);
        }

        public static void Warning(string warning)
        {
            if (Level.HasFlag(LogLevel.Warnings))
                LogOutput(ConsoleColor.Yellow, warning);
        }

        public static void Debug(string debug)
        {
            if (Level.HasFlag(LogLevel.Debugging))
                LogOutput(ConsoleColor.Magenta, debug);
        }

        private static void LogOutput(ConsoleColor color, string log)
        {
            string logPreamble = $"[{_timeStamp}][{_logName}]: ";

            Console.ForegroundColor = color;
            Console.Write(logPreamble);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(log);
        }
    }
}
