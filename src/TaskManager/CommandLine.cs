using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Mono.Options;

namespace TaskManager
{
    /// <summary>
    /// TaskManager's CLI.
    /// </summary>
    public static class CommandLine
    {
        /// <summary>
        /// Runs TaskManager in a console window.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void Run(string[] args)
        {
            ShowConsoleWindow();
            RunFromConsole(args);
        }

        /// <summary>
        /// Runs TaskManager in a console window.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "All errors should be write to catch.")]
        private static void RunFromConsole(string[] args)
        {
            ShowHeader();
            TaskManagerOptions options;

            try
            {
                options = TaskManagerOptions.Create("TaskManager.exe", args);
            }
            catch(Exception ex)
            {
				Show("Argument parsing error: ");
			    Show(ex.Message);
			    Show("Try `TaskManager --help` for more information");
                Environment.Exit(3);
                return;
            }

            if (options.ShowHelp)
            {
                Show(options.HelpText);
                return;
            }

            WaitUserInteraction(options, "Press ENTER to locate task modules.");
            TaskManagerService.LogInfo("Initializing service...");

            try
            {
                Show("Event log: {0}", options.EventLog.GetType().Name);
                Show("Stats strategy: {0}", options.StatsStrategy.GetType().Name);

                TaskSupervisor.Initialize(options.StatsStrategy);
                TaskManagerService.Initialize(options.EventLog);
                ModuleSupervisor.Initialize();
            }
            catch (Exception e)
            {
                TaskManagerService.LogError("Unable to initialize service.", e);
                WaitUserInteraction(options);
                return;
            }

            WaitUserInteraction(options, "Press ENTER to start.");

            try
            {
                TaskManagerService.LogInfo("Service successfully started...");
                ModuleSupervisor.Execute();

                if (options.NonStop && options.NonStopWait > 0)
                {
                    Show("Waiting {0} milliseconds to tasks execution...", options.NonStopWait);
                    Thread.Sleep(options.NonStopWait);
                }
            }
            catch (Exception e)
            {
                TaskManagerService.LogError("Unable to start service.", e);
                WaitUserInteraction(options);

                return;
            }

            // If you are debugging, you can freeze the main thread here.
            WaitUserInteraction(options, "Press ENTER to stop.");

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
                WaitUserInteraction(options);
                return;
            }

            WaitUserInteraction(options, "Press ENTER to finish.");
        }

        private static void WaitUserInteraction(TaskManagerOptions options, string message = null)
        {
            if (!options.NonStop)
            {
                Show(String.IsNullOrEmpty(message) ? "Press ENTER to continue" : message);
                Console.ReadLine();
            }
        }

        private static void ShowHeader()
        {
            NewLine();
            Show("TaskManager CLI v{0} (Windows Service name: {1})", typeof(CommandLine).Assembly.GetName().Version, TaskManagerInstaller.SERVICE_NAME);
            NewLine();
        }
      
        private static void Show(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }

        private static void NewLine(int lines = 1)
        {
            for (int i = 0; i < lines; i++)
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Shows the console window.
        /// </summary>
        private static void ShowConsoleWindow()
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
        private static void HideConsoleWindow()
        {
            var handle = NativeMethods.GetConsoleWindow();

            if (handle != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(handle, NativeMethods.SW_HIDE);
            }
        }        
    }
}
