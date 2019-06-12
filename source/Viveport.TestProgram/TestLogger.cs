using System;
using System.Collections.Generic;
using System.Linq;
namespace Viveport.TestProgram
{
    public static class TestLogger
    {
        enum LogType
        {
            Debug, Success, Warnning, Error
        }

        private static void ShowMessageWithFormat(LogType type, string methodName, string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            switch (type)
            {
                case LogType.Debug:
                default:
                    Console.BackgroundColor = ConsoleColor.DarkBlue;                    
                    break;
                case LogType.Success:
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    break;
                case LogType.Warnning:
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    break;
                case LogType.Error:
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    break;
            }

            Console.Write("[VIVEPORT][SDK]");
            Console.Write(string.Format("{0}", methodName != null ? "[" + methodName + "]" : ""));
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine(" " + message);
        }

        public static void Debug(string methodName, string message)
        {
            ShowMessageWithFormat(LogType.Debug, methodName, message);
        }

        public static void Success(string methodName, string message)
        {
            ShowMessageWithFormat(LogType.Success, methodName, message);
        }

        public static void Warnning(string methodName, string message)
        {
            ShowMessageWithFormat(LogType.Warnning, methodName, message);
        }

        public static void Error(string methodName, string message)
        {
            ShowMessageWithFormat(LogType.Error, methodName, message);
        }
    }
}
