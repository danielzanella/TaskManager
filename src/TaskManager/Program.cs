namespace TaskManager
{
    using System;
    using System.Runtime.InteropServices;
    using System.ServiceProcess;

    /// <summary>
    /// The Windows service.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
        public static void Main()
        {
            if (System.Environment.UserInteractive)
            {
                // If you are debugging and haven't installed TaskManager as a service, make sure your login has enough priviledges to create an EventLog source.
                ShowConsoleWindow();

                RunFromConsole();
            }
            else
            {
                ServiceBase.Run(new ServiceBase[] { new TaskManagerService() });
            }
        }

        /// <summary>
        /// Runs the task manager from the console.
        /// </summary>
        private static void RunFromConsole()
        {
            Console.WriteLine("Press ENTER to locate task modules.");

            Console.ReadLine();

            TaskManagerService.LogInfo("Initializing service...");
            try
            {
                TaskSupervisor.Initialize();
                ModuleSupervisor.Initialize();
            }
            catch (Exception e)
            {
                TaskManagerService.LogError("Unable to initialize service.", e);
                return;
            }

            Console.WriteLine("Press ENTER to start.");

            Console.ReadLine();

            try
            {
                TaskManagerService.LogInfo("Service successfully started...");

                ModuleSupervisor.Execute();
            }
            catch (Exception e)
            {
                TaskManagerService.LogError("Unable to start service.", e);
                return;
            }

            Console.WriteLine("Press ENTER to stop.");

            // If you are debugging, you can freeze the main thread here.
            Console.ReadLine();

            TaskManagerService.LogInfo("Stopping service...");
            try
            {
                ModuleSupervisor.Shutdown();
                TaskSupervisor.Shutdown();
                TaskManagerService.LogInfo("Service successfully stopped...");
            }
            catch (Exception e)
            {
                TaskManagerService.LogError("Unable to stop service.", e);
                return;
            }

            Console.WriteLine("Press ENTER to finish.");
            Console.ReadLine();
        }

        /// <summary>
        /// Shows the console window.
        /// </summary>
        public static void ShowConsoleWindow()
        {
            var handle = NativeMethods.GetConsoleWindow();

            if (handle == IntPtr.Zero)
            {
                NativeMethods.AllocConsole();
            }
            else
            {
                NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOW);
            }
        }

        /// <summary>
        /// Hides the console window.
        /// </summary>
        public static void HideConsoleWindow()
        {
            var handle = NativeMethods.GetConsoleWindow();

            if (handle != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(handle, NativeMethods.SW_HIDE);
            }
        }
    }
}