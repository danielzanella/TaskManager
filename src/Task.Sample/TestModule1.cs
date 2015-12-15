namespace Task.Sample
{
    using System;
    using System.Threading;
    using System.Xml.Linq;
    using TaskManager.Common;

    /// <summary>
    /// A sample module.
    /// </summary>
    public class TestModule1 : ITaskModule
    {
        static int id = 0;
        int myId = 0;
        Random r;
        int totalAttempts = 0;
        int counter = 0;

        /// <summary>
        /// Executes some work.
        /// </summary>
        /// <returns>True if there is more work to be done, false otherwise.</returns>
        public bool Execute()
        {
            int i = r.Next(1, 10);

            counter++;

            Thread.Sleep(i * 1000);

            if (i == 6) throw new Exception("Test");

            if (counter < totalAttempts)
            {
                return true;
            }
            else
            {
                counter = 0;
                return false;
            }
        }

        /// <summary>
        /// Configures the task with contents from the xml configuration file.
        /// </summary>
        /// <param name="xml">The xml node.</param>
        public void Configure(XElement xml)
        {
            id++;
            myId = id;
            r = new Random(myId + DateTime.Now.Second);

            totalAttempts = r.Next(5, 30);
        }
    }
}