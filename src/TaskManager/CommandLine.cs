using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Mono.Options;

namespace TaskManager
{
    /// <summary>
    /// TaskManager's command line.
    /// </summary>
    public static class CommandLine
    {
        #region Fields
        private static string _eventLog = "Console";
        private static string _stats = "Memory";
        private static bool _nonStop;
        private static int _nonStopWait;
        private static bool _showHelp;
        #endregion

        /// <summary>
        /// Runs the command line.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Run(string[] args)
        {
            ShowConsoleWindow();
            RunFromConsole(args);
        }

        /// <summary>
        /// Runs the task manager from the console.
        /// </summary>
		private static void RunFromConsole(string[] args)
        {
            var options = BuildOptions();

            if (!ParseArguments(options, args))
            {
                return;
            }

            WaitUserInteraction("Press ENTER to locate task modules.");
            TaskManagerService.LogInfo("Initializing service...");

            try
            {
                TaskSupervisor.Initialize(ArgumentsHelper.CreateStatsStrategy(_stats));
                TaskManagerService.Initialize(ArgumentsHelper.CreateEventLog(_eventLog));
                ModuleSupervisor.Initialize();
            }
            catch (Exception e)
            {
                TaskManagerService.LogError("Unable to initialize service.", e);
                WaitUserInteraction();
                return;
            }

            WaitUserInteraction("Press ENTER to start.");

            try
            {
                TaskManagerService.LogInfo("Service successfully started...");
                ModuleSupervisor.Execute();

                if (_nonStop && _nonStopWait > 0)
                {
                    Console.WriteLine("Waiting {0} milliseconds to tasks execution...", _nonStopWait);
                    Thread.Sleep(_nonStopWait);
                }
            }
            catch (Exception e)
            {
                TaskManagerService.LogError("Unable to start service.", e);
                WaitUserInteraction();

                return;
            }

            // If you are debugging, you can freeze the main thread here.
            WaitUserInteraction("Press ENTER to stop.");

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

            WaitUserInteraction("Press ENTER to finish.");
        }

        private static void WaitUserInteraction(string message = null)
        {
            if (!_nonStop)
            {
                Console.WriteLine(String.IsNullOrEmpty(message) ? "Press ENTER to continue" : message);
                Console.ReadLine();
            }
        }

        private static void ShowHeader(string webApiUrl, string version)
        {
            NewLine();
            Show("TaskManager {0} by Daniel Zanella (@daniel_zanella)", version);
            Show(webApiUrl);
            NewLine();
        }

        private static OptionSet BuildOptions()
        {
            return new OptionSet()
            {
                "Usage: ",
                "   TaskManager -e <event log> -s <stats> -non-stop",
                string.Empty,
                "Options:",
                {
                    "e|event-log=", "the event log. Available values are: Console and Windows. Default is: Console.", e => _eventLog = e
                },
                {
                    "s|stats=", "the stats strategy. Available values are: Memory and PerformanceCounter. Default is: Memory.", s => _stats = s
                },
                {
                    "non-stop", "if should wait for user interaction", n => _nonStop = n != null
                },
                {
                    "non-stop-wait=", "the time in milliseconds to wait to tasks run when in non-stop mode", n => _nonStopWait = Convert.ToInt32(n)
                },
                {
                    "h|help", "show this message and exit", v => _showHelp = v != null
                },
                string.Empty,
                string.Empty,
                "Samples:",
                "TaskManager -e Windows -s PerformanceCounter -non-stop",
                string.Empty,
                "TaskManager -non-stop"
            };
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "All errors should be write to console.")]
        private static bool ParseArguments(OptionSet optionsSet, string[] args)
        {
            try
            {
                try
                {
                    optionsSet.Parse(args);
                }
                catch (OptionException e)
                {
                    Console.Write("Argument parsing error: ");
                    Show(e.Message);
                    Show("Try `TaskManager --help` for more information");
                    return false;
                }

                if (_showHelp)
                {
                    optionsSet.WriteOptionDescriptions(Console.Out);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Show("ERROR: {0}", ex.Message);
                Environment.Exit(3);
            }

            return true;
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
