using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DSLink.Logger
{
    /// <summary>
    /// Abstract class to implement logger.
    /// </summary>
    public abstract class BaseLogger
    {
        /// <summary>
        /// Highest level of log to print.
        /// </summary>
        public readonly LogLevel ToPrint;

        protected BaseLogger(LogLevel toPrint)
        {
            ToPrint = toPrint;
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="caller">Name of calling member</param>
        /// <param name="lineNumber">Line number of calling member</param>
        public void Error(string message, [CallerFilePath] string caller = null, [CallerLineNumber] int lineNumber = 0)
        {
            if (ToPrint.DoesPrint(LogLevel.Error))
            {
                PrintError(Format(LogLevel.Error, message, caller, lineNumber));
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="caller">Name of calling member</param>
        /// <param name="lineNumber">Line number of calling member</param>
        public void Warning(string message, [CallerFilePath] string caller = null, [CallerLineNumber] int lineNumber = 0)
        {
            if (ToPrint.DoesPrint(LogLevel.Warning))
            {
                PrintError(Format(LogLevel.Warning, message, caller, lineNumber));
            }
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="caller">Name of calling member</param>
        /// <param name="lineNumber">Line number of calling member</param>
        public void Info(string message, [CallerFilePath] string caller = null, [CallerLineNumber] int lineNumber = 0)
        {
            if (ToPrint.DoesPrint(LogLevel.Info))
            {
                Print(Format(LogLevel.Info, message, caller, lineNumber));
            }
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="caller">Name of calling member</param>
        /// <param name="lineNumber">Line number of calling member</param>
        public void Debug(string message, [CallerFilePath] string caller = null, [CallerLineNumber] int lineNumber = 0)
        {
            if (ToPrint.DoesPrint(LogLevel.Debug))
            {
                Print(Format(LogLevel.Debug, message, caller, lineNumber));
            }
        }

        /// <summary>
        /// Formats a string for output to logs.
        /// </summary>
        /// <param name="logLevel">Specified level of log</param>
        /// <param name="message">Content of the message</param>
        /// <param name="caller">Name of calling member</param>
        /// <param name="lineNumber">Line number of calling member</param>
        private static string Format(LogLevel logLevel, string message, string caller, int lineNumber)
        {
            var fileName = caller.Substring(caller.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            return string.Format("[{0}][{1}:{2}][{3}] {4}", DateTime.Now.ToString("MM-dd HH:mm:ss.fff"),
                fileName, lineNumber, logLevel, message);
        }

        protected abstract void Print(string message);
        protected abstract void PrintError(string message);
    }
}

