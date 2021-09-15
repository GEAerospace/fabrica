// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace GEAviation.Fabrica.Utility
{
    /// <summary>
    /// This interface represents objects that can perform logging functionality.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Gets/sets the current default <see cref="LogLevel"/>. Used for
        /// logging functions that do not take a <see cref="LogLevel"/> as an
        /// argument.
        /// </summary>
        LogLevel LogLevel { get; set; }

        /// <summary>
        /// Logs the given message as an error.
        /// </summary>
        /// <param name="aMessage">
        /// The message to log as an error.
        /// </param>
        void logError(string aMessage);

        /// <summary>
        /// Logs the given message as an warning.
        /// </summary>
        /// <param name="aMessage">
        /// The message to log as an warning.
        /// </param>
        void logWarning(string aMessage);

        /// <summary>
        /// Logs the given message as an informational message.
        /// </summary>
        /// <param name="aMessage">
        /// The message to log as an informational message.
        /// </param>
        void logMessage(string aMessage);

        /// <summary>
        /// Logs the given exception.
        /// </summary>
        /// <param name="aException">
        /// The exception to log.
        /// </param>
        void logException(Exception aException);

        /// <summary>
        /// Logs the given exception along with the specified message.
        /// </summary>
        /// <param name="aException">
        /// The exception to log.
        /// </param>
        /// <param name="aMessage">
        /// The message to log.
        /// </param>
        void logException(Exception aException, string aMessage);

        /// <summary>
        /// Logs the given message as an error, at the specified log level.
        /// </summary>
        /// <param name="aLogLevel">
        /// The log level to perform the logging at.
        /// </param>
        /// <param name="aMessage">
        /// The message to log as an error.
        /// </param>
        void logError(LogLevel aLogLevel, string aMessage);

        /// <summary>
        /// Logs the given message as an warning, at the specified log level.
        /// </summary>
        /// <param name="aLogLevel">
        /// The log level to perform the logging at.
        /// </param>
        /// <param name="aMessage">
        /// The message to log as an warning.
        /// </param>
        void logWarning(LogLevel aLogLevel, string aMessage);

        /// <summary>
        /// Logs the given message as an informational message, at the specified log level.
        /// </summary>
        /// <param name="aLogLevel">
        /// The log level to perform the logging at.
        /// </param>
        /// <param name="aMessage">
        /// The message to log as an informational message.
        /// </param>
        void logMessage(LogLevel aLogLevel, string aMessage);

        /// <summary>
        /// Logs the given exception, at the specified log level.
        /// </summary>
        /// <param name="aLogLevel">
        /// The log level to perform the logging at.
        /// </param>
        /// <param name="aException">
        /// The exception to log.
        /// </param>
        void logException(LogLevel aLogLevel, Exception aException);

        /// <summary>
        /// Logs the given exception along with the specified message, at the specified log level.
        /// </summary>
        /// <param name="aLogLevel">
        /// The log level to perform the logging at.
        /// </param>
        /// <param name="aException">
        /// The exception to log.
        /// </param>
        /// <param name="aMessage">
        /// The message to log.
        /// </param>
        void logException(LogLevel aLogLevel, Exception aException, string aMessage);
    }
}
