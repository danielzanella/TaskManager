namespace TaskManager
{
    using System.ServiceProcess;

    /// <summary>
    /// The Windows service.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
		public static void Main(string[] args)
        {
            if (System.Environment.UserInteractive)
            {
                // If you are debugging and haven't installed TaskManager as a service, make sure your login has enough priviledges to create an EventLog source.
                CommandLine.Run(args);
            }
            else
            {
                ServiceBase.Run(new ServiceBase[] { new TaskManagerService() });
            }
        }        
    }
}