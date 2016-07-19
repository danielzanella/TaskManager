using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager
{
	/// <summary>
	/// An IEventLog's implementation that use Windows Event Log to write event messages.
	/// </summary>
    public class WindowsEventLog : IEventLog
    {
        ///// <summary>
        ///// The EventLog instance.
        ///// </summary>
        private static EventLog _eventLog = new EventLog(TaskManagerService.LogName, ".", TaskManagerService.LogSource);

		/// <summary>
		/// Write an information event.
		/// </summary>
		/// <param name="message">The event message.</param>
        public void WriteInfo(string message)
        {
            _eventLog.WriteEntry(message, EventLogEntryType.Information);
        }

		/// <summary>
		/// Write an error event.
		/// </summary>
		/// <param name="message">The event message.</param>
        public void WriteError(string message)
        {
            _eventLog.WriteEntry(message, EventLogEntryType.Error);
        }

		/// <summary>
		/// Write a warning event.
		/// </summary>
		/// <param name="message">The event message.</param>
        public void WriteWarning(string message)
        {
            _eventLog.WriteEntry(message, EventLogEntryType.Warning);
        }

		/// <summary>
		/// Close the event log.
		/// </summary>
        public void Close()
        {
            _eventLog.Close();            
        }
    }
}