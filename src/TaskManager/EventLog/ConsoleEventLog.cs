using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager
{
    /// <summary>
    /// IEventLog implementation that writes to the standard console output.
    /// </summary>
    public class ConsoleEventLog : IEventLog
    {
		/// <summary>
		/// Writes an information event.
		/// </summary>
		/// <param name="message">The event message.</param>
        public void WriteInfo(string message)
        {
            Console.WriteLine(message);
        }

		/// <summary>
		/// Writes an error event.
		/// </summary>
		/// <param name="message">The event message.</param>
        public void WriteError(string message)
        {
            Console.WriteLine("***" + message);
        }

		/// <summary>
		/// Writes a warning event.
		/// </summary>
		/// <param name="message">The event message.</param>
        public void WriteWarning(string message)
        {
            Console.WriteLine("***" + message);
        }

		/// <summary>
		/// Closes the event log.
		/// </summary>
        public void Close()
        {            
        }
    }
}