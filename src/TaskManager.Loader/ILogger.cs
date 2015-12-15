namespace TaskManager
{
    using System;

    /// <summary>
    /// Describes a logging component.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Writes a message to the log output.
        /// </summary>
        /// <param name="message">The message.</param>
        void Log(string message);

        /// <summary>
        /// Writes a message and exception information to the log output.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">The exception.</param>
        void Log(string message, Exception ex);

        /// <summary>
        /// Writes exception information to the log output.
        /// </summary>
        /// <param name="ex">The exception.</param>
        void Log(Exception ex);
    }
}