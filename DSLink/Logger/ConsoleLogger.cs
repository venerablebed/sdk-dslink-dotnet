using System;

namespace DSLink.Logger
{
    /// <summary>
    /// Standard logger for platforms that support using Console and Console.Error.
    /// </summary>
    /// <inheritdoc />
    public class ConsoleLogger : BaseLogger
    {
        public ConsoleLogger(LogLevel toPrint) : base(toPrint)
        {
        }

        /// <summary>
        /// Prints a message to the console.
        /// </summary>
        /// <param name="message">Message to write to console</param>
        protected override void Print(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Prints an error message to the console.
        /// </summary>
        /// <param name="message">Message to write to console</param>
        protected override void PrintError(string message)
        {
            Console.Error.WriteLine(message);
        }
    }
}

