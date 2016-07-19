using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager
{
    /// <summary>
    /// Defines an interface to an event log used by TaskManager.
    /// </summary>
    public interface IEventLog
    {
        /// <summary>
        /// Write an information event.
        /// </summary>
        /// <param name="message">The event message.</param>
        void WriteInfo(string message);

        /// <summary>
        /// Write an error event.
        /// </summary>
        /// <param name="message">The event message.</param>
        void WriteError(string message);

        /// <summary>
        /// Write a warning event.
        /// </summary>
        /// <param name="message">The event message.</param>
        void WriteWarning(string message);

        /// <summary>
        /// Close the event log.
        /// </summary>
        void Close();
    }
}
