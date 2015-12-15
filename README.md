TaskManager
======
C# Windows Service for scheduling execution of multiple tasks. Allows easy deployment of updates and server management.


Features
------
- Multiple schedules per task (ex: can run a task more often during certain hours);
- Controlled execution bursts (ex: execute a task up to 10 times per second, until there is no more work available, then wait 10 seconds before next execution)
- Multi-threaded, uses a thread pool, terminates excess threads when idle;
- Plugin architecture, isolates tasks in their own AppDomain and allows updating existing task files without requiring the whole service/other tasks to be shut down (installing new tasks still requires a service restart);
- Monitors filesystem, changes to a file used by a task already loaded (binaries, config file, etc) restarts only that task;
- Provides EventLog (information and errors) and Performance Counter data (number of threads, number of tasks, timeouts/sec, etc);


Quick Instructions
------
1. Copy service to a folder and install using ServiceInstall.cmd;
2. Create a new application library project, referencing TaskManager.Common.dll;
3. Create a new class, implement ITaskModule;
4. Create a XML file, using the same name as the library (same file name as the compiled binary, changing the extension from .dll to .xml);

        <?xml version="1.0" encoding="utf-8" ?>
        <tasks>
            <task type="Namespace.ClassName" defaults-go-here>
                <schedules>
                    <!-- schedule-specific configuration goes here -->
                </schedules>
                <configuration>
                    <!-- task configuration goes here -->
                </configuration>
            </task>
        </tasks>

5. Default configuration attributes are specified in the `task` element (if no schedules are specified in the `schedule` element, the task will run periodically throughout the whole day using these defaults);
6. Specific schedules are specified inside the `schedules` element (one `schedule` element per interval; uses same configuration attributes as the task element, along with a `from` and `to` attribute to specify at which time of the day that specific schedule starts and ends);
7. Elements in the `configuration` element are passed as-is to the `Configure` method;
8. Build the project and copy the output files to a new folder inside the TaskManager service folder;
9. Start the service; Detected tasks will be recorded in the service Event Log.


Task Configuration Attributes
------

These attributes can be used only inside the `task` element:

Attribute  | Data Type | Default    | Description
---------- | :-------: | :--------: | -----------
type       |  String   |      -     | Full name of the task class (Namespace.ClassName)
culture    |  String   |      -     | Culture that will be set to Thread.CurrentCulture before task execution starts
delayStart | TimeSpan  | 5 minutes  | Upon service startup / task module reload, sets how long the scheduler should wait before start executing this task


These attributes can be used only inside the `schedule` element:

Attribute  | Data Type | Default    | Description
---------- | :-------: | :--------: | -----------
from       | TimeSpan  |  00:00:00  | Time of the day (in local time) when the schedule starts
to         | TimeSpan  |  23:59:59  | Time of the day (in local time) when the schedule ends


These attributes can be used inside both the `task` and `schedule` elements:

Attribute  | Data Type | Default    | Description
---------- | :-------: | :--------: | -----------
timeUnit   | TimeSpan  |  1 second  | Defines an execution interval, or cycle
maxRuns    |   Int32   |      1     | Sets how many times the task can be executed inside the interval defined in timeUnit (ex: runs up to 10 times per second)
wait       | TimeSpan  | 0 seconds  | Sets how long the task should wait before next execution when either it returns false (no more work to execute) or throws an Exception
timeout    | TimeSpan  | 15 minutes | How long to wait before execution is aborted and rescheduled (after waiting `wait` time)
sla        | TimeSpan  | 5 seconds  | How long the task is allowed to be late; after that, if no threads are available to start executing the task, a new thread is spawned and the task executes immediately
spawn      |   Int32   |      1     | Sets how many times the same task instance is queued for execution (task should be thread-safe to use this feature)


Providing an App.config file for the library is also supported.


FAQ
------
1. What about cron-style schedules?
   * Back in the day, it was requested that the scheduler be configured like that (due to the bursting feature, IIRC). Might add cron-style schedules in the future.


Samples
------
See src/Task.Sample for usage examples.


License
------
Licensed under the The MIT License (MIT).
In others words, you can use this library for developement any kind of software: open source, commercial, proprietary and alien.

