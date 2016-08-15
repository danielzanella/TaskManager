namespace TaskManager
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.IO.Compression;
    using System.Reflection;
    using System.Threading;
    using TaskManager.Configuration;

    /// <summary>
    /// Loads and unloads modules, and listen for module file changes to reload the module when necessary.
    /// </summary>
    internal static class ModuleSupervisor
    {
        /// <summary>
        /// Default module zip file extension.
        /// </summary>
        public const string ZipFileDefaultExtension = ".zip";

        /// <summary>
        /// Default module file extension.
        /// </summary>
        public const string ModuleFileDefaultExtension = ".dll";

        /// <summary>
        /// Default module configuration file extension.
        /// </summary>
        public const string ConfigFileDefaultExtension = ".xml";

        /// <summary>
        /// List of known loaded modules.
        /// </summary>
        private static List<ModuleData> _moduleList;

        /// <summary>
        /// Enumeration lock.
        /// </summary>
        private static object _moduleListLock;

        /// <summary>
        /// File system watcher.
        /// </summary>
        private static FileSystemWatcher _fileSystemWatcher;

        /// <summary>
        /// The time when the next reload should occur.
        /// </summary>
        private static DateTime _reload;

        /// <summary>
        /// Loader lock.
        /// </summary>
        private static object _reloadLock;

        /// <summary>
        /// Reloading thread.
        /// </summary>
        private static Thread _reloadThread;

        /// <summary>
        /// Initializes static members of the <see cref="ModuleSupervisor"/> class.
        /// </summary>
        static ModuleSupervisor()
        {
            _moduleListLock = new object();
            _reloadLock = new object();
            _reload = DateTime.Now;
            _reloadThread = null;
        }

        /// <summary>
        /// Initializes the supervisor.
        /// </summary>
        public static void Initialize()
        {
            _moduleList = new List<ModuleData>();

            lock (_moduleListLock)
            {
                LoadAndConfigure(false);
            }

            if (_moduleList.Count < 1)
            {
                throw new Exception("Unable to locate task modules.");
            }

            StartMonitoringFileSystem();
        }

        /// <summary>
        /// Loads all modules.
        /// </summary>
        public static void Execute()
        {
            if (null == _moduleList)
            {
                throw new Exception("TaskManager failed to initialize.");
            }

            lock (_moduleListLock)
            {
                foreach (ModuleData module in _moduleList)
                {
                    foreach (TaskWrapper task in module.Tasks)
                    {
                        TaskManagerService.Logger.Log(string.Format("Registering task '{0}'...", task.TaskName));
                        TaskSupervisor.ScheduleTask(task);
                    }
                }
            }
        }

        /// <summary>
        /// Unloads all modules.
        /// </summary>
        public static void Shutdown()
        {
            if (null == _moduleList)
            {
                throw new Exception("TaskManager failed to initialize.");
            }

            _fileSystemWatcher.EnableRaisingEvents = false;

            if (_reloadThread != null)
            {
                _reloadThread.Abort();
                _reloadThread.Join();
            }

            lock (_moduleListLock)
            {
                for (int i = _moduleList.Count - 1; i >= 0; i--)
                {
                    StopModule(_moduleList[i]);
                }
            }
        }

        #region Module loading

        /// <summary>
        /// Locates and configures all modules.
        /// </summary>
        /// <param name="startImmediately">True if execution should be started immediately after configuring all modules, false if modules should be configured but not started.</param>
        public static void LoadAndConfigure(bool startImmediately)
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules");

            string cfgPath = ConfigurationManager.AppSettings["Modules.Path"];

            if (null != cfgPath)
            {
                if (!Directory.Exists(cfgPath))
                {
                    TaskManagerService.Logger.Log(string.Format("Could not find the specified path \"{0}\", using defaults.", cfgPath));
                }
                else
                {
                    basePath = cfgPath;
                }
            }

            List<string> filesToScan = new List<string>();

            filesToScan.AddRange(Directory.GetFiles(basePath, "*" + ModuleFileDefaultExtension, SearchOption.AllDirectories));
            filesToScan.AddRange(Directory.GetFiles(basePath, "*" + ZipFileDefaultExtension, SearchOption.AllDirectories));

            TaskManagerService.Logger.Log(string.Format("Scanning path '{0}' for modules...", basePath));

            List<string> possibleFiles = new List<string>();

            foreach (string file in filesToScan)
            {
                if (Path.GetExtension(file) != ModuleFileDefaultExtension && Path.GetExtension(file) != ZipFileDefaultExtension)
                {
                    continue;
                }

                if (Path.GetDirectoryName(file) + @"\" == basePath && Path.GetExtension(file) != ZipFileDefaultExtension)
                {
                    continue;
                }

                bool alreadyLoaded = false;

                lock (_moduleListLock)
                {
                    foreach (ModuleData module in _moduleList)
                    {
                        if (module.DllFile == file || module.ZipFile == file)
                        {
                            alreadyLoaded = true;
                            break;
                        }
                    }
                }

                if (alreadyLoaded)
                {
                    continue;
                }

                string xmlFile = Path.ChangeExtension(file, ConfigFileDefaultExtension);

                if (File.Exists(xmlFile))
                {
                    possibleFiles.Add(file);
                }

                string filePath = Path.GetDirectoryName(file);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                string overridePath = Path.Combine(filePath, fileNameWithoutExtension);

                if (Path.GetExtension(file).ToLower() == ModuleFileDefaultExtension)
                {
                    xmlFile = Path.Combine(overridePath, Path.ChangeExtension(Path.GetFileName(file), ConfigFileDefaultExtension));

                    if (File.Exists(xmlFile))
                    {
                        possibleFiles.Add(file);
                    }
                }
                else if (Path.GetExtension(file).ToLower() == ZipFileDefaultExtension)
                {
                    possibleFiles.Add(file);
                }
            }

            foreach (string moduleFile in possibleFiles)
            {
                switch (Path.GetExtension(moduleFile).ToLower())
                {
                    case ModuleFileDefaultExtension:
                        {
                            string xmlFile = Path.ChangeExtension(moduleFile, ConfigFileDefaultExtension);

                            if (File.Exists(xmlFile))
                            {
                                if (xmlFile.IsValidConfigurationFile())
                                {
                                    LoadAndConfigure(moduleFile, xmlFile, startImmediately);
                                }
                                else
                                {
                                    TaskManagerService.Logger.Log(string.Format("Unable to load module '{0}': exception caught while loading configuration file.", moduleFile));
                                }
                            }

                            break;
                        }

                    case ZipFileDefaultExtension:
                        {
                            try
                            {
                                LoadZipAndConfigure(moduleFile, startImmediately);
                            }
                            catch (Exception e)
                            {
                                TaskManagerService.Logger.Log(string.Format("Unable to load zipped module '{0}'.", moduleFile), e);
                            }

                            break;
                        }
                }
            }

            TaskManagerService.Logger.Log(string.Format("{0} modules found.", _moduleList.Count));
        }

        /// <summary>
        /// Loads a module from a zip file.
        /// </summary>
        /// <param name="zipFile">The path to the zip file.</param>
        /// <param name="startImmediately">True if execution should be started immediately after configuring the module, false if module should be configured but not started.</param>
        private static void LoadZipAndConfigure(string zipFile, bool startImmediately)
        {
            string tempPath = null;

            int i = 0;

            while (Directory.Exists(tempPath = Path.Combine(Path.Combine(Path.GetTempPath(), "TaskManager"), Path.GetRandomFileName())))
            {
                i++;
                if (i == 10) throw new Exception("Failed to create a new temporary folder.");
            }

            if (!tempPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                tempPath += Path.DirectorySeparatorChar;
            }

            string overridePath = Path.Combine(Path.GetDirectoryName(zipFile), Path.GetFileNameWithoutExtension(zipFile)) + Path.DirectorySeparatorChar;

            string loaderDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TaskManager.Loader.dll");
            string tempLoaderDll = Path.Combine(tempPath, "TaskManager.Loader.dll");
            string commonDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TaskManager.Common.dll");
            string tempCommonDll = Path.Combine(tempPath, "TaskManager.Common.dll");

            List<string> dependencies = new List<string>();

            DirectoryInfo directoryInfo = Directory.CreateDirectory(tempPath);
            try
            {
                using (Stream fileStream = File.Open(zipFile, FileMode.Open, FileAccess.Read))
                {
                    using (ZipArchive zip = new ZipArchive(fileStream, ZipArchiveMode.Read))
                    {
                        var directory = zip.Entries;

                        foreach (var compressedFile in directory)
                        {
                            string destinationFile = Path.Combine(tempPath, compressedFile.FullName);
                            string overrideFile = Path.Combine(overridePath, compressedFile.FullName);
                            dependencies.Add(overrideFile);

                            compressedFile.ExtractToFile(destinationFile, true);
                        }
                    }
                }

                if (Directory.Exists(overridePath))
                {
                    foreach (string overrideFile in Directory.GetFiles(overridePath, "*.*", SearchOption.AllDirectories))
                    {
                        if (!dependencies.Contains(overrideFile))
                        {
                            dependencies.Add(overrideFile);
                        }

                        dependencies.Add(Path.Combine(overridePath, overrideFile.Replace(tempPath, string.Empty)));

                        string relativeName = overrideFile.Replace(overridePath, string.Empty);
                        string destination = Path.Combine(tempPath, relativeName);
                        string destinationPath = Path.GetDirectoryName(destination);

                        if (!Directory.Exists(destinationPath))
                        {
                            Directory.CreateDirectory(destinationPath);
                        }

                        File.Copy(overrideFile, destination, true);
                    }
                }

                List<string> possibleFiles = new List<string>();
                foreach (string moduleFile in Directory.GetFiles(tempPath, "*" + ModuleFileDefaultExtension, SearchOption.AllDirectories))
                {
                    string xmlFile = Path.ChangeExtension(moduleFile, ConfigFileDefaultExtension);

                    if (File.Exists(xmlFile))
                    {
                        possibleFiles.Add(moduleFile);
                    }
                }

                File.Copy(loaderDll, tempLoaderDll, true);
                File.Copy(commonDll, tempCommonDll, true);

                foreach (string dllFile in possibleFiles)
                {
                    string xmlFile = Path.ChangeExtension(dllFile, ConfigFileDefaultExtension);

                    if (File.Exists(xmlFile))
                    {
                        if (!xmlFile.IsValidConfigurationFile())
                        {
                            continue;
                        }

                        string modulePath = Path.GetDirectoryName(dllFile);

                        string[] files = Directory.GetFiles(modulePath, "*.*", SearchOption.AllDirectories);

                        AppDomainSetup domainSetup = new AppDomainSetup();
                        domainSetup.ShadowCopyFiles = "true";
                        domainSetup.ApplicationBase = tempPath;
                        domainSetup.ConfigurationFile = dllFile + ".config";

                        AppDomain domain = AppDomain.CreateDomain(dllFile, null, domainSetup);

                        TaskWrapper[] tasks = new TaskWrapper[0];

                        try
                        {
                            TaskManagerService.Logger.Log(string.Format("Module found: '{0}', configuration file: '{1}', scanning assembly for tasks...", dllFile, xmlFile));

                            AssemblyName loaderAssemblyName = AssemblyName.GetAssemblyName(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TaskManager.Loader.dll"));
                            Loader loader = (Loader)domain.CreateInstanceAndUnwrap(loaderAssemblyName.ToString(), "TaskManager.Loader");

                            tasks = loader.LoadAndConfigure(dllFile, xmlFile, TaskManagerService.Logger, AppDomain.CurrentDomain.BaseDirectory);

                            ModuleData newModule = new ModuleData();
                            newModule.BasePath = overridePath;
                            newModule.Tasks = tasks;
                            newModule.DllFile = dllFile;
                            newModule.XmlFile = xmlFile;
                            newModule.ZipFile = zipFile;
                            newModule.ZipDirectory = tempPath;
                            newModule.Files = new List<string>(files);
                            newModule.Files.AddRange(dependencies);
                            newModule.Files.Add(zipFile);
                            newModule.Domain = domain;

                            if (startImmediately)
                            {
                                foreach (TaskWrapper task in newModule.Tasks)
                                {
                                    TaskSupervisor.ScheduleTask(task);
                                }
                            }

                            _moduleList.Add(newModule);
                        }
                        catch (Exception ex)
                        {
                            foreach (TaskWrapper task in tasks)
                            {
                                try
                                {
                                    TaskSupervisor.RemoveTask(task);
                                }
                                catch
                                {
                                }
                            }

                            AppDomain.Unload(domain);

                            TaskManagerService.Logger.Log(string.Format("Unable to load module '{0}' from zipped file '{1}'.", dllFile, zipFile), ex);
                        }
                    }
                }

                foreach (ModuleData module in _moduleList)
                {
                    if (module.ZipFile == zipFile)
                    {
                        return;
                    }
                }

                throw new Exception(string.Format("Unable to find tasks in zipped file '{0}'.", zipFile));
            }
            catch
            {
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch (Exception ex)
                {
                    TaskManagerService.Logger.Log(string.Format("Unable to remove temporary directory '{0}'.", tempPath), ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Loads a module.
        /// </summary>
        /// <param name="dllFile">The path to the module DLL file.</param>
        /// <param name="xmlFile">The path to the module XML configuration file.</param>
        /// <param name="startImmediately">True if execution should be started immediately after configuring the module, false if module should be configured but not started.</param>
        private static void LoadAndConfigure(string dllFile, string xmlFile, bool startImmediately)
        {
            string modulePath = Path.GetDirectoryName(dllFile);
            if (!modulePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                modulePath += Path.DirectorySeparatorChar;
            }

            string[] files = Directory.GetFiles(modulePath, "*.*", SearchOption.AllDirectories);

            AppDomainSetup domainSetup = new AppDomainSetup();
            domainSetup.ShadowCopyFiles = "true";
            domainSetup.ApplicationBase = modulePath;
            domainSetup.ConfigurationFile = dllFile + ".config";

            AppDomain domain = AppDomain.CreateDomain(dllFile, null, domainSetup);

            TaskManagerService.Logger.Log(string.Format("Module found: '{0}', configuration file: '{1}', scanning assembly for tasks...", dllFile, xmlFile));

            if (modulePath != AppDomain.CurrentDomain.BaseDirectory)
            {
                string loaderDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TaskManager.Loader.dll");
                string tempLoaderDll = Path.Combine(modulePath, "TaskManager.Loader.dll");
                string commonDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TaskManager.Common.dll");
                string tempCommonDll = Path.Combine(modulePath, "TaskManager.Common.dll");

                File.Copy(loaderDll, tempLoaderDll, true);
                File.Copy(commonDll, tempCommonDll, true);
            }

            AssemblyName loaderAssemblyName = AssemblyName.GetAssemblyName(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TaskManager.Loader.dll"));
            Loader loader = (Loader)domain.CreateInstanceAndUnwrap(loaderAssemblyName.ToString(), "TaskManager.Loader");

            TaskWrapper[] tasks = loader.LoadAndConfigure(dllFile, xmlFile, TaskManagerService.Logger, AppDomain.CurrentDomain.BaseDirectory);

            ModuleData newModule = new ModuleData();
            newModule.BasePath = Path.GetDirectoryName(dllFile);
            newModule.Tasks = tasks;
            newModule.DllFile = dllFile;
            newModule.XmlFile = xmlFile;
            newModule.Files = new List<string>(files);
            newModule.Domain = domain;

            if (startImmediately)
            {
                foreach (TaskWrapper task in newModule.Tasks)
                {
                    TaskSupervisor.ScheduleTask(task);
                }
            }

            _moduleList.Add(newModule);
        }

        /// <summary>
        /// Unloads a module.
        /// </summary>
        /// <param name="moduleData">The module.</param>
        private static void StopModule(ModuleData moduleData)
        {
            try
            {
                for (int i = moduleData.Tasks.Length - 1; i >= 0; i--)
                {
                    TaskManagerService.Logger.Log(string.Format("Stopping task '{0}'...", moduleData.Tasks[i].TaskName));
                    TaskSupervisor.RemoveTask(moduleData.Tasks[i]);
                }

                TaskManagerService.Logger.Log(string.Format("Unloading AppDomain '{0}'...", moduleData.Domain.FriendlyName));
                AppDomain.Unload(moduleData.Domain);

                TaskManagerService.Logger.Log("AppDomain successfully unloaded.");

                if (moduleData.ZipFile != null && moduleData.ZipDirectory != null)
                {
                    Directory.Delete(moduleData.ZipDirectory, true);
                }

                _moduleList.Remove(moduleData);
            }
            catch (Exception e)
            {
                TaskManagerService.Logger.Log(string.Format("Exception caught while shutting down module '{0}'", moduleData.DllFile), e);
                throw;
            }
        }

        #endregion

        #region File system monitoring

        /// <summary>
        /// Starts monitoring the file system for changes to module files.
        /// </summary>
        private static void StartMonitoringFileSystem()
        {
            _fileSystemWatcher = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory);
            _fileSystemWatcher.Changed += new FileSystemEventHandler(FileSystem_Event);
            _fileSystemWatcher.Created += new FileSystemEventHandler(FileSystem_Event);
            _fileSystemWatcher.Renamed += new RenamedEventHandler(FileSystem_Renamed);
            _fileSystemWatcher.Deleted += new FileSystemEventHandler(FileSystem_Event);
            _fileSystemWatcher.IncludeSubdirectories = true;
            _fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Handler for the Renamed event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void FileSystem_Renamed(object sender, RenamedEventArgs e)
        {
            HandleFileSystemEvent(e, e.OldFullPath, e.FullPath);
        }

        /// <summary>
        /// Handler for the generic event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void FileSystem_Event(object sender, FileSystemEventArgs e)
        {
            HandleFileSystemEvent(e, e.FullPath, null);
        }

        /// <summary>
        /// Handles the file system event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        /// <param name="fileName">The path to the file.</param>
        /// <param name="relatedFileName">The path to the related file.</param>
        private static void HandleFileSystemEvent(FileSystemEventArgs e, string fileName, string relatedFileName)
        {
            try
            {
                string directory = Path.GetDirectoryName(fileName);

                string extension = Path.GetExtension(fileName);
                if (null != extension)
                {
                    extension = extension.ToLower();
                }

                string extension2 = Path.GetExtension(relatedFileName);
                if (null != extension2)
                {
                    extension2 = extension2.ToLower();
                }

                if (directory + @"\" == AppDomain.CurrentDomain.BaseDirectory && (extension != ZipFileDefaultExtension && extension2 != ZipFileDefaultExtension))
                {
                    return;
                }

                lock (_moduleListLock)
                {
                    for (int i = _moduleList.Count - 1; i >= 0; i--)
                    {
                        bool restartModule = false;
                        string file = null;

                        if (_moduleList[i].DllFile == fileName || _moduleList[i].XmlFile == fileName || (_moduleList[i].ZipFile != null && _moduleList[i].ZipFile == fileName) || (null != fileName && _moduleList[i].Files.Contains(fileName)))
                        {
                            restartModule = true;
                            file = fileName;
                        }

                        if (_moduleList[i].DllFile == relatedFileName || _moduleList[i].XmlFile == relatedFileName || (_moduleList[i].ZipFile != null && _moduleList[i].ZipFile == relatedFileName) || (null != relatedFileName && _moduleList[i].Files.Contains(relatedFileName)))
                        {
                            restartModule = true;
                            file = relatedFileName;
                        }

                        if (restartModule)
                        {
                            string eventDescription = "modified";
                            if (e.ChangeType == WatcherChangeTypes.Created)
                            {
                                eventDescription = "created";
                            }
                            else if (e.ChangeType == WatcherChangeTypes.Deleted)
                            {
                                eventDescription = "deleted";
                            }
                            else if (e.ChangeType == WatcherChangeTypes.Renamed)
                            {
                                eventDescription = "renamed";
                            }

                            TaskManagerService.Logger.Log(string.Format("File '{2}' of module '{0}' was {1}, restarting module...", _moduleList[i].Domain.FriendlyName, eventDescription, file));

                            StopModule(_moduleList[i]);
                        }
                    }
                }

                lock (_reloadLock)
                {
                    _reload = DateTime.Now.AddSeconds(5);

                    if (_reloadThread == null)
                    {
                        _reloadThread = new Thread(new ThreadStart(Reload));
                        _reloadThread.Name = "Reload thread";
                        _reloadThread.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                TaskManagerService.Logger.Log("Unable to watch filesystem for events.", ex);
            }
        }

        /// <summary>
        /// Entry point for the reload thread.
        /// </summary>
        private static void Reload()
        {
            while (true)
            {
                lock (_reloadLock)
                {
                    if (_reload <= DateTime.Now)
                    {
                        break;
                    }
                }

                Thread.Sleep(500);
            }

            LoadAndConfigure(true);

            lock (_reloadLock)
            {
                _reloadThread = null;
            }
        }

        #endregion

        /// <summary>
        /// Represents internal module information.
        /// </summary>
        private class ModuleData
        {
            /// <summary>
            /// Gets or sets the path to the module files.
            /// </summary>
            public string BasePath { get; set; }

            /// <summary>
            /// Gets or sets the path to the module DLL file.
            /// </summary>
            public string DllFile { get; set; }

            /// <summary>
            /// Gets or sets the path to the module configuration file.
            /// </summary>
            public string XmlFile { get; set; }

            /// <summary>
            /// Gets or sets the path to the module zip file.
            /// </summary>
            public string ZipFile { get; set; }

            /// <summary>
            /// Gets or sets the path to the temporary directory where module files have been extracted to.
            /// </summary>
            public string ZipDirectory { get; set; }

            /// <summary>
            /// Gets or sets the list of files related to the module.
            /// </summary>
            public List<string> Files { get; set; }

            /// <summary>
            /// Gets or sets the list of task instances.
            /// </summary>
            public TaskWrapper[] Tasks { get; set; }

            /// <summary>
            /// Gets or sets the module AppDomain.
            /// </summary>
            public AppDomain Domain { get; set; }
        }
    }
}
