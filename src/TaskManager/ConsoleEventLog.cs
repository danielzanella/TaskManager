using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager
{
    /// <summary>
    /// An IEventLog's implementation that write events messages to the Console.
    /// </summary>
    public class ConsoleEventLog : IEventLog
    {
		/// <summary>
		/// Write an information event.
		/// </summary>
		/// <param name="message">The event message.</param>
        public void WriteInfo(string message)
        {
            Console.WriteLine(message);
        }

		/// <summary>
		/// Write an error event.
		/// </summary>
		/// <param name="message">The event message.</param>
        public void WriteError(string message)
        {
            Console.WriteLine("***" + message);
        }

		/// <summary>
		/// Write a warning event.
		/// </summary>
		/// <param name="message">The event message.</param>
        public void WriteWarning(string message)
        {
            Console.WriteLine("***" + message);
        }

		/// <summary>
		/// Close the event log.
		/// </summary>
        public void Close()
        {            
        }
    }
}