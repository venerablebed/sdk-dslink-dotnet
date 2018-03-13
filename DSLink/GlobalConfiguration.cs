using System;
using DSLink.Logger;

namespace DSLink
{
    public static class GlobalConfiguration
    {
        public static Type LogType = typeof(ConsoleLogger);
        public static LogLevel LogLevel = LogLevel.Info;
    }
}