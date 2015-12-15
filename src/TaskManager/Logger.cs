namespace TaskManager
{
    using System;
    
    /// <summary>
    /// Implements a logger that is shared between AppDomains.
    /// </summary>
    [Serializable]
    public sealed class Logger : MarshalByRefObject, ILogger
    {
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="text">The message.</param>
        public void Log(string text)
        {
            TaskManagerService.LogInfo(text);
        }

        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="text">The message.</param>
        /// <param name="ex">The exception.</param>
        public void Log(string text, Exception ex)
        {
            TaskManagerService.LogError(text, ex);
        }

        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="ex">The exception.</param>
        public void Log(Exception ex)
        {
            TaskManagerService.LogError("Exception caught", ex);
        }

        /// <summary>
        /// Keeps the reference alive throughout the AppDomain lifetime.
        /// </summary>
        /// <returns>Null object.</returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
