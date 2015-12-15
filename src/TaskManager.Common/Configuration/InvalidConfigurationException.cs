namespace TaskManager.Configuration
{
    using System;

    /// <summary>
    /// Represents configuration file errors.
    /// </summary>
    [Serializable]
    public class InvalidConfigurationException : Exception
    {
        /// <summary>
        /// Default error message.
        /// </summary>
        private const string ErrorMessage = "Invalid configuration.";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TaskManager.Configuration.InvalidConfigurationException"/> class.
        /// </summary>
        public InvalidConfigurationException()
            : base(ErrorMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TaskManager.Configuration.InvalidConfigurationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public InvalidConfigurationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TaskManager.Configuration.InvalidConfigurationException"/> class.
        /// </summary>
        /// <param name="inner">The inner exception.</param>
        public InvalidConfigurationException(Exception inner)
            : base(ErrorMessage, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TaskManager.Configuration.InvalidConfigurationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="inner">The inner exception.</param>
        public InvalidConfigurationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TaskManager.Configuration.InvalidConfigurationException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected InvalidConfigurationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
