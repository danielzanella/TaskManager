namespace Task.Sample
{
	using System;
	using System.Threading;
	using System.Xml.Linq;
	using TaskManager.Common;

	/// <summary>
	/// A sample module.
	/// </summary>
	public class TestModule2: ITaskModule
	{
		/// <summary>
		/// Executes some work.
		/// </summary>
		/// <returns>True if there is more work to be done, false otherwise.</returns>
		public bool Execute()
		{			
			Console.WriteLine ("TaskModule2 starting...");
			Thread.Sleep(250);
			Console.WriteLine ("TaskModule2 ended.");

			return false;
		}

		/// <summary>
		/// Configures the task with contents from the xml configuration file.
		/// </summary>
		/// <param name="xml">The xml node.</param>
		public void Configure(XElement xml)
		{			
		}
	}
}