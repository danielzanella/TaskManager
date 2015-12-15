namespace TaskManager.Common
{
    using System.Xml.Linq;

    /// <summary>
    /// Defines the module interface.
    /// </summary>
    public interface ITaskModule
    {
        /// <summary>
        /// Configures the module instance.
        /// </summary>
        /// <param name="configuration">The configuration XML element.</param>
        void Configure(XElement configuration);

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>True if the tasks executed successfully and is aware of more work to be processed; false otherwise.</returns>
        bool Execute();
    }
}
