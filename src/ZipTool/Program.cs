namespace MZipTool
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.IO.Compression;
    using System.Threading;
    using System.Xml;

    /// <summary>
    /// Small command line utility for managing module zip files.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The entry point.
        /// </summary>
        /// <param name="args">Run with /? for help.</param>
        public static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "/?")
            {
                ShowHelp(true);
                return;
            }

            Queue<string> parameters = new Queue<string>(args);

            string op = parameters.Dequeue().ToLower();
            if (!op.StartsWith("/") || (op != "/c" && op != "/u" && op != "/i" && op != "/ix" && op != "/?"))
            {
                InvalidCommand();
                return;
            }

            if (op == "/c")
            {
                FileCommand(parameters, "c");
                return;
            }

            if (op == "/u")
            {
                FileCommand(parameters, "u");
                return;
            }

            if (op.StartsWith("/i"))
            {
                InspectFile(parameters, op.EndsWith("x"));
                return;
            }

            InvalidCommand();
        }

        /// <summary>
        /// Shows the invalid command message.
        /// </summary>
        private static void InvalidCommand()
        {
            Console.WriteLine("Invalid command.");
            ShowHelp(false);
        }

        /// <summary>
        /// Shows the invalid parameter message.
        /// </summary>
        private static void InvalidOption()
        {
            Console.WriteLine("One or more parameters are invalid, or missing parameters.");
            ShowHelp(false);
        }

        /// <summary>
        /// Shows help.
        /// </summary>
        /// <param name="all">Indicates whether the full help text should be emitted.</param>
        private static void ShowHelp(bool all)
        {
            if (all)
            {
                Console.WriteLine();
                Console.WriteLine("ZipTool - Module zip maintenance tool.");
            }

            Console.WriteLine();
            Console.WriteLine("Usage: ziptool /c zip_file /a:path [/a...]");
            Console.WriteLine("       ziptool /u zip_file [/a:path] [/r:filter] [/a|/r...]");
            Console.WriteLine("       ziptool /i[x] zip_file [zip_file...]");
            Console.WriteLine("       ziptool /gui");
            Console.WriteLine("       ziptool /?");
            Console.WriteLine();

            if (all)
            {
                Console.WriteLine("Commands:");
                Console.WriteLine();
                Console.WriteLine("  /c          - Create module zip file.");
                Console.WriteLine("                Requires specifying files with /a");
                Console.WriteLine();
                Console.WriteLine("  /u          - Update module zip file.");
                Console.WriteLine("                Requires specifying files to add or remove with /a or /r");
                Console.WriteLine();
                Console.WriteLine("  /i          - Open the specified file and show information regarding");
                Console.WriteLine("                module and tasks.");
                Console.WriteLine("  /ix         - Extra information, like task schedules from the");
                Console.WriteLine("                configuration file.");
                Console.WriteLine();
                Console.WriteLine("  /?          - This message.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine();
                Console.WriteLine("  /a:path     - Update or add the specified files to the zip file.");
                Console.WriteLine("                (allows wildcards, use quotes if path contains spaces)");
                Console.WriteLine();
                Console.WriteLine("  /r:filter   - Remove matched files from the zip file.");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine();
                Console.WriteLine("  Creates a zip file:");
                Console.WriteLine("    ziptool /c module.zip /a:*.*");
                Console.WriteLine();
                Console.WriteLine("  Updates the configuration file:");
                Console.WriteLine("    ziptool /u module.zip /a:\"C:\\Program Files\\Module.xml\"");
                Console.WriteLine();
                Console.WriteLine("  Remove files:");
                Console.WriteLine("    ziptool /u module.zip /r:*.pdb /r:*.txt");
                Console.WriteLine();
                Console.WriteLine("  Enumerates tasks in files, along with task schedules:");
                Console.WriteLine("    ziptool /ix module1.zip process*.zip");
                Console.WriteLine();
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Executes a command.
        /// </summary>
        /// <param name="options">The arguments.</param>
        /// <param name="op">The operation specified in the command line.</param>
        /// <remarks>Available ops: "c" to create a new file, "u" to update an existing file.</remarks>
        private static void FileCommand(Queue<string> options, string op)
        {
            string fileName = options.Dequeue();

            if (null == fileName || string.Empty == fileName)
            {
                InvalidCommand();
            }

            if (options.Count < 1)
            {
                InvalidOption();
            }

            List<Operation> toAdd = new List<Operation>();

            while (options.Count > 0)
            {
                string parm = options.Dequeue();

                if (!parm.ToLower().StartsWith("/a:") && op == "c")
                {
                    InvalidOption();
                    return;
                }

                if (!parm.ToLower().StartsWith("/a:") && !parm.ToLower().StartsWith("/r:"))
                {
                    InvalidOption();
                    return;
                }

                string type = parm.Substring(1, 1).ToLower();

                parm = parm.Substring(3);

                int idx = parm.IndexOf("/");

                if (idx >= 0 && idx < parm.Length)
                {
                    parm = ReturnParameter(ref options, parm, idx);
                }

                toAdd.Add(new Operation { Type = type, Filter = parm });
            }

            if (File.Exists(fileName) && op == "c")
            {
                Console.WriteLine("Error: specified file already exists. (Maybe you meant /u ?)");
                return;
            }

            if (op == "c")
            {
                Console.Write("Creating file " + fileName + " ...");
            }
            else if (op == "u")
            {
                Console.Write("Updating file " + fileName + " ...");
            }

            ZipArchive zip = null;
            Stream theStream = null;

            try
            {
                if (op == "c")
                {
                    theStream = System.IO.File.Create(fileName);

                    zip = new ZipArchive(theStream, ZipArchiveMode.Create);
                }
                else if (op == "u")
                {
                    theStream = System.IO.File.Open(fileName, FileMode.Open, FileAccess.ReadWrite);

                    zip = new ZipArchive(theStream, ZipArchiveMode.Update);
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine();
                Console.WriteLine("Error: unable to locate file " + Path.GetDirectoryName(fileName));
                return;
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine();
                Console.WriteLine("Error: unable to locate path " + Path.GetDirectoryName(fileName));
                return;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine();
                Console.WriteLine("Error: access denied to path " + Path.GetDirectoryName(fileName));
                return;
            }

            int result = 0;

            try
            {
                try
                {
                    result = Dispatch(toAdd, zip);
                }
                finally
                {
                    if (null != zip)
                    {
                        zip.Dispose();
                    }

                    if (null != theStream)
                    {
                        theStream.Dispose();
                    }
                }
            }
            catch
            {
            }

            if (0 == result)
            {
                Console.WriteLine("Error: No files found.");

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }

        /// <summary>
        /// Executes file operations.
        /// </summary>
        /// <param name="commands">The list of operations to be executed.</param>
        /// <param name="zip">Reference to the opened zip archive.</param>
        /// <returns>The number of changes executed.</returns>
        private static int Dispatch(List<Operation> commands, ZipArchive zip)
        {
            int changes = 0;

            try
            {
                Console.WriteLine(" OK!");
                foreach (Operation op in commands)
                {
                    switch (op.Type)
                    {
                        case "a":
                            {
                                string basePath = Path.GetDirectoryName(op.Filter);

                                if (null == basePath || string.Empty == basePath)
                                {
                                    basePath = Directory.GetCurrentDirectory();
                                }

                                try
                                {
                                    foreach (string file in Directory.GetFiles(basePath, Path.GetFileName(op.Filter)))
                                    {
                                        Console.Write("  Adding file " + file + " ...");

                                        try
                                        {
                                            string fileName = Path.GetFileName(file);

                                            if (zip.Mode == ZipArchiveMode.Update)
                                            {
                                                var existingEntry = zip.Entries.SingleOrDefault(e => e.Name.ToLowerInvariant() == fileName.ToLowerInvariant());
                                                if (null != existingEntry) existingEntry.Delete();
                                            }

                                            zip.CreateEntryFromFile(file, fileName, CompressionLevel.Optimal);
                                            changes++;
                                            Console.WriteLine(" OK!");
                                        }
                                        catch (FileNotFoundException)
                                        {
                                            Console.WriteLine();
                                            Console.WriteLine("Error: unable to locate file " + file);
                                        }
                                        catch (UnauthorizedAccessException)
                                        {
                                            Console.WriteLine();
                                            Console.WriteLine("Error: access denied to file " + file);
                                        }
                                        catch (IOException)
                                        {
                                            Console.WriteLine();
                                            Console.WriteLine("Error: caught exception while trying to read file " + file);
                                        }

                                        Thread.Sleep(1);
                                    }
                                }
                                catch (DirectoryNotFoundException)
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("Error: unable to locate path " + basePath);
                                    return changes;
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("Error: access denied to path " + basePath);
                                    return changes;
                                }

                                break;
                            }

                        case "r":
                            {
                                bool hasStart = !op.Filter.StartsWith("*");
                                string[] components = op.Filter.Split('*');
                                string filterStart = (components[0] != string.Empty) ? components[0] : null;
                                string filterEnd = (components[1] != string.Empty) ? components[1] : null;

                                List<ZipArchiveEntry> toRemove = new List<ZipArchiveEntry>();

                                foreach (var entry in zip.Entries)
                                {
                                    if (filterStart == null || (filterStart != null && entry.FullName.StartsWith(filterStart, StringComparison.InvariantCultureIgnoreCase)))
                                    {
                                        if (filterEnd == null || (filterEnd != null && entry.FullName.EndsWith(filterEnd, StringComparison.InvariantCultureIgnoreCase)))
                                        {
                                            toRemove.Add(entry);
                                            Console.WriteLine("  Removing file " + entry.FullName + " ...");
                                        }
                                    }
                                }

                                if (toRemove.Count > 0)
                                {
                                    try
                                    {
                                        foreach (var entry in toRemove)
                                        {
                                            entry.Delete();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                    }

                                    changes++;
                                }

                                break;
                            }
                    }
                }
            }
            finally
            {
                if (null != zip) 
                {
                    zip.Dispose();
                }
            }

            return changes;
        }

        /// <summary>
        /// Parses an operation from the command line.
        /// </summary>
        /// <param name="parameters">The arguments.</param>
        /// <param name="parm">The parameter.</param>
        /// <param name="idx">The index.</param>
        /// <returns>A parameter.</returns>
        /// <remarks>This was written many years ago...</remarks>
        private static string ReturnParameter(ref Queue<string> parameters, string parm, int idx)
        {
            string toReturn = parm.Substring(idx);

            parm = parm.Substring(0, idx);

            string[] queue = new string[parameters.Count + 1];

            int i = 1;

            while (parameters.Count > 0)
            {
                queue[i] = parameters.Dequeue();
                i++;
            }

            queue[0] = toReturn;

            for (i = 0; i < queue.Length; i++)
            {
                parameters.Enqueue(queue[i]);
            }

            return parm;
        }

        /// <summary>
        /// Inspects a file and attempts to figure out stuff about it.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="extended">Boolean indicating whether extended information (like schedules) should be emitted.</param>
        private static void InspectFile(Queue<string> parameters, bool extended)
        {
            if (parameters.Count == 0)
            {
                InvalidCommand();
                return;
            }

            List<string> files = new List<string>();

            while (parameters.Count > 0)
            {
                string filter = parameters.Dequeue();

                string basePath = Path.GetDirectoryName(filter);

                if (null == basePath || string.Empty == basePath)
                {
                    basePath = Directory.GetCurrentDirectory();
                }

                files.AddRange(Directory.GetFiles(basePath, Path.GetFileName(filter)));
            }

            foreach (string file in files)
            {
                List<string> dlls = new List<string>();

                Dictionary<string, ZipArchiveEntry> xmls = new Dictionary<string, ZipArchiveEntry>();

                Console.WriteLine("Inspecting file " + file + " ...");

                bool found = false;
                using (Stream theFile = System.IO.File.Open(file, FileMode.Open, FileAccess.Read))
                {
                    using (ZipArchive zip = new ZipArchive(theFile, ZipArchiveMode.Read))
                    {
                        Console.WriteLine("  Loading modules and configurations...");
                        Console.WriteLine();

                        foreach (var entry in zip.Entries)
                        {
                            if (Path.GetExtension(entry.FullName).ToLower() == ".dll")
                            {
                                dlls.Add(entry.FullName);
                            }
                            else if (Path.GetExtension(entry.FullName).ToLower() == ".xml")
                            {
                                xmls[entry.FullName] = entry;
                            }
                        }

                        List<ZipArchiveEntry> configs = new List<ZipArchiveEntry>();
                        foreach (string dll in dlls)
                        {
                            if (xmls.ContainsKey(Path.ChangeExtension(dll, "xml")))
                            {
                                configs.Add(xmls[Path.ChangeExtension(dll, "xml")]);
                            }
                        }

                        foreach (var config in configs)
                        {
                            using (var ms = config.Open())
                            {
                                XmlDocument doc = new XmlDocument();

                                doc.Load(ms);

                                if (doc.DocumentElement.Name == "tasks")
                                {
                                    found = true;

                                    Console.WriteLine("  Module found: " + Path.ChangeExtension(config.FullName, "dll"));
                                    Console.WriteLine();

                                    XmlNodeList tasks = doc.SelectNodes("/tasks/task");

                                    foreach (XmlNode task in tasks)
                                    {
                                        if (task.NodeType != XmlNodeType.Element) continue;

                                        Console.WriteLine("    Task found: " + task.Attributes["type"].Value);

                                        if (extended)
                                        {
                                            string defaultTimeout = task.Attributes["timeout"].ValueOrNull() ?? "00:15:00";
                                            string defaultSla = task.Attributes["sla"].ValueOrNull() ?? "00:00:05";
                                            string defaultSpawn = task.Attributes["spawn"].ValueOrNull() ?? "1";
                                            string defaultTimeUnit = task.Attributes["timeUnit"].ValueOrNull() ?? "00:00:01";
                                            string defaultMaxRuns = task.Attributes["maxRuns"].ValueOrNull() ?? "1";
                                            string defaultWait = task.Attributes["wait"].ValueOrNull() ?? "00:00:00";

                                            Console.WriteLine("      Default number of workers: " + defaultSpawn);
                                            Console.WriteLine("      Default number of executions: " + defaultMaxRuns);
                                            Console.WriteLine("      Default time unit: " + defaultTimeUnit);
                                            Console.WriteLine("      Default wait if no work left: " + defaultWait);
                                            Console.WriteLine("      Execution timeout: " + defaultTimeout);
                                            Console.WriteLine("      Max wait before ensuring execution: " + defaultSla);

                                            if (task["schedules"] != null && task["schedules"].ChildNodes.Count > 0)
                                            {
                                                foreach (XmlNode schedule in task["schedules"])
                                                {
                                                    if (schedule.NodeType != XmlNodeType.Element) continue;

                                                    if (schedule.Name == "schedule")
                                                    {
                                                        string from = schedule.Attributes["from"].ValueOrNull() ?? "00:00:00";
                                                        string to = schedule.Attributes["to"].ValueOrNull() ?? "23:59:59";
                                                        string timeUnit = schedule.Attributes["timeUnit"].ValueOrNull();// ?? defaultTimeUnit;
                                                        string maxRuns = schedule.Attributes["maxRuns"].ValueOrNull();// ?? defaultMaxRuns;
                                                        string wait = schedule.Attributes["wait"].ValueOrNull();// ?? defaultWait;
                                                        string spawns = schedule.Attributes["spawn"].ValueOrNull();// ?? defaultSpawn;
                                                        string sla = schedule.Attributes["sla"].ValueOrNull();// ?? defaultSla;
                                                        string timeout = schedule.Attributes["timeout"].ValueOrNull();// ?? defaultTimeout;

                                                        List<string> infos = new List<string>();

                                                        if (null != from) infos.Add(string.Format("from {0} ", from));
                                                        if (null != to) infos.Add(string.Format("to {0}", to));
                                                        if (infos.Count > 0) infos.Add(", ");
                                                        if (null != spawns) infos.Add(string.Format("spawns {0} worker(s)", spawns));
                                                        if (infos.Count > 0 && infos.Last() != ", ") infos.Add(", ");
                                                        if (null != maxRuns) infos.Add(string.Format("up to {0} execution(s)", maxRuns));
                                                        if (null != maxRuns && null != timeUnit) infos.Add(" ");
                                                        if (null != timeUnit) infos.Add(string.Format("every {0}", timeUnit));
                                                        if (infos.Count > 0 && infos.Last() != ", ") infos.Add(", ");
                                                        if (null != wait) infos.Add(string.Format("waits {0} when no work left", wait));
                                                        if (infos.Count > 0 && infos.Last() != ", ") infos.Add(", ");
                                                        if (null != timeout) infos.Add(string.Format("times out after {0}", timeout));
                                                        if (infos.Count > 0 && infos.Last() != ", ") infos.Add(", ");
                                                        if (null != sla) infos.Add(string.Format("waits up to {0} before ensuring execution", sla));
                                                        if (infos.Count > 0 && infos.Last() == ", ") infos.RemoveAt(infos.Count - 1);

                                                        Console.Write("      "); //From {0} to {1}, spawns {5} workers, up to {2} executions every {3}, waits {4} when no work left", from, to, maxRuns, timeUnit, wait, spawns);
                                                        Console.WriteLine(string.Join("", infos.ToArray()).Capitalize());
                                                    }
                                                }
                                            }

                                            Console.WriteLine();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!found)
                {
                    Console.WriteLine("  No modules found (or missing configuration file).");
                }

                Console.WriteLine();
            }
        }
        
        /// <summary>
        /// Represents a file operation.
        /// </summary>
        private struct Operation
        {
            /// <summary>
            /// The type of operation.
            /// </summary>
            public string Type;

            /// <summary>
            /// The file filter mask.
            /// </summary>
            public string Filter;
        }
    }

    public static class Extensions
    {
        public static string ValueOrNull(this XmlAttribute attribute)
        {
            if (null == attribute) return null;
            if (string.IsNullOrWhiteSpace(attribute.Value)) return null;
            return attribute.Value;
        }

        public static string Capitalize(this string value)
        {
            return string.Concat(value.Substring(0, 1).ToUpper(), value.Substring(1));
        }
    }
}
