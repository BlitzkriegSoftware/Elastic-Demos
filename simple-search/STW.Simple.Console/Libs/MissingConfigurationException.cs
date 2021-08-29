using System;

namespace STW.Simple.Console.Libs
{
    /// <summary>
    /// Missing Configuration Exception
    /// </summary>
    [Serializable]
    public class MissingConfigurationException : Exception
    {
        /// <summary>
        /// Configuration Item that is missing
        /// </summary>
        public string ConfigurationItem { get; set; }

        /// <summary>
        /// CTOR
        /// </summary>
        public MissingConfigurationException() : base() { }

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="message">(sic)</param>
        public MissingConfigurationException(string message) : base(message) { }

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="message">(sic)</param>
        /// <param name="innerException">(sic)</param>
        public MissingConfigurationException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="configurationItem">Configuration Item that is missing</param>
        /// <param name="message">(sic)</param>
        public MissingConfigurationException(string configurationItem, string message) : base(message)
        {
            this.ConfigurationItem = configurationItem;
        }

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="configurationItem">Configuration Item that is missing</param>
        /// <param name="message">(sic)</param>
        /// <param name="innerException">(sic)</param>
        public MissingConfigurationException(string configurationItem, string message, Exception innerException) : base(message, innerException)
        {
            this.ConfigurationItem = configurationItem;
        }

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected MissingConfigurationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
            ConfigurationItem = info?.GetString(nameof(this.ConfigurationItem));
        }

        /// <summary>
        /// Get Object Data
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            info?.AddValue(nameof(ConfigurationItem), this.ConfigurationItem);
            base.GetObjectData(info, context);
        }

    }
}
