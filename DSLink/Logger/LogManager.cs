using System;

namespace DSLink.Logger
{
    public static class LogManager
    {
        public static BaseLogger GetLogger()
        {
            return (BaseLogger)Activator.CreateInstance(GlobalConfiguration.LogType, GlobalConfiguration.LogLevel);
        }
    }
}