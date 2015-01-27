using System;

namespace NLog
{
    public static class LoggerExtensionMethods
    {
        public static void DebugException(this Logger logger, Exception exception)
        {
            logger.Log(LogLevel.Debug, String.Empty, exception);
        }

        public static void ErrorException(this Logger logger, Exception exception)
        {
            logger.Log(LogLevel.Error, String.Empty, exception);
        }

        public static void WarnException(this Logger logger, Exception exception)
        {
            logger.Log(LogLevel.Warn, String.Empty, exception);
        }
    }
}
