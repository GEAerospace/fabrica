// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace GEAviation.Fabrica.Utility
{
    /// <summary>
    /// Provides a base implementation of the ILogger interface, standardizing
    /// many aspects of the log format.
    /// </summary>
    public abstract class LoggerBase : ILogger
    {
        private object mLockObject = null;

        /// <summary>
        /// This gets an object that should be used to thread-lock this Logger.
        /// </summary>
        protected object LockObject {
            get
            {
                if (mLockObject == null)
                {
                    this.LockObject = new object();
                }
                return mLockObject;
            }
            private set
            {
                mLockObject = value;
            }
        }

        public Func<LogLevel, string, string> LogLineFormatter { get; set; } = delegate(LogLevel aLevel, string aMessage)
        {
            DateTime lNow = DateTime.UtcNow;
            string lFormatted = String.Format("{0} UTC [{1,-10}]: {2}", lNow.ToString("yyyy/MM/dd HH:mm:ss.fff"), aLevel.ToString().ToUpper(), aMessage);

            return lFormatted;
        };

        /// <summary>
        /// Logs a message with the specified prefix, at the specified log level.
        /// </summary>
        /// <param name="aLogLevel">
        /// The level at which to log this message.
        /// </param>
        /// <param name="aPrefix">
        /// The prefix to display with this logged message.
        /// </param>
        /// <param name="aMessage">
        /// The message to log with a prefix.
        /// </param>
        protected virtual void logWithPrefix(LogLevel aLogLevel, string aPrefix, string aMessage)
        {
            if (aPrefix == null)
            {
                throw new ArgumentNullException(nameof(aPrefix));
            }

            if (aMessage == null)
            {
                throw new ArgumentNullException(nameof(aMessage));
            }

            this.logMessageThreadLock(aLogLevel, aPrefix + " > " + aMessage);
        }

        private LogLevel mLogLevel = LogLevel.Normal;

        /// <inheritdoc/>
        public LogLevel LogLevel
        {
            get
            {
                lock (this.LockObject)
                {
                    return mLogLevel;
                }
            }
            set
            {
                lock (this.LockObject)
                {
                    mLogLevel = value;
                }
            }
        }

        /// <inheritdoc/>
        public virtual void logError(LogLevel aLogLevel, string aMessage)
        {
            this.logWithPrefix(aLogLevel, "ERROR", aMessage);
        }

        /// <inheritdoc/>
        public virtual void logWarning(LogLevel aLogLevel, string aMessage)
        {
            this.logWithPrefix(aLogLevel, "WARNING", aMessage);
        }

        /// <inheritdoc/>
        public virtual void logMessage(LogLevel aLogLevel, string aMessage)
        {
            this.logMessageThreadLock(aLogLevel, aMessage);
        }

        /// <summary>
        /// When implemented in a child class, all other logging functions provided by
        /// LoggerBase are funnelled into this call. Only messages meeting/exceeding the LogLevel will
        /// get passed into this function.
        /// </summary>
        /// <param name="aLogLevel">
        /// The level at which this message should be logged.
        /// </param>
        /// <param name="aMessage">
        /// The message to log.
        /// </param>
        public abstract void logMessageOutput(LogLevel aLogLevel, string aMessage);

        /// <summary>
        /// LoggingBase's functions are all funnelled to this function. The base implementation
        /// thread locks, checks that the desired message meets the current log-level, and then attempts
        /// to call the abstract logMesssage() with it if it meets the level.
        /// 
        /// Child classes that implement this base may consider overriding this function to force all
        /// log levels to log, no matter the level, or to create separate logs for each log level, etc.
        /// </summary>
        /// <param name="aLogLevel">
        /// The level at which to log this message.
        /// </param>
        /// <param name="aMessage">
        /// The message to log.
        /// </param>
        protected virtual void logMessageInternal(LogLevel aLogLevel, string aMessage)
        {
            if (meetsLevel(aLogLevel))
            {
                string lFinalMessage = aMessage;
                if (this.LogLineFormatter != null)
                {
                    lFinalMessage = this.LogLineFormatter(aLogLevel, aMessage);
                }
                this.logMessageOutput(aLogLevel, lFinalMessage);
            }
        }

        /// <summary>
        /// This function wraps the call to logMessageInternal() with a thread lock.
        /// Call this function whenever an internal need to funnel a message to 
        /// logMessageInternal occurs.
        /// </summary>
        /// <param name="aLogLevel"></param>
        /// <param name="aMessage"></param>
        protected void logMessageThreadLock(LogLevel aLogLevel, string aMessage)
        {
            lock (this.LockObject)
            {
                this.logMessageInternal(aLogLevel, aMessage);
            }
        }

        /// <summary>
        /// Helper function for logging Exception messages.
        /// </summary>
        /// <param name="aLogLevel">
        /// The level at which to log this message.
        /// </param>
        /// <param name="aMessage">
        /// The message to log.
        /// </param>
        protected virtual void logException(LogLevel aLogLevel, string aMessage)
        {
            this.logWithPrefix(aLogLevel, "EXCEPTION", aMessage);
        }

        /// <inheritdoc/>
        public virtual void logException(LogLevel aLogLevel, Exception aException)
        {
            this.logException(aLogLevel, aException, "");
        }

        /// <inheritdoc/>
        public virtual void logException(LogLevel aLogLevel, Exception aException, string aMessage)
        {
            if(string.IsNullOrWhiteSpace(aMessage))
            { 
                this.logException(aLogLevel, aException.ToString());
            }
            else
            {
                this.logException(aLogLevel, $"{aMessage}\r\n{aException.ToString()}");
            }
        }

        /// <summary>
        /// Checks if the supplied Log Level meets or exceeds the currently set LogLevel.
        /// If the provided Log Level meets/exceeds (i.e. should be logged), this functions
        /// returns true.
        /// </summary>
        /// <param name="aLogLevel">
        /// The log level of the message.
        /// </param>
        /// <returns>
        /// True if the message meets/exceeds the log level and should be logged. False
        /// otherwise.
        /// </returns>
        protected bool meetsLevel(LogLevel aLogLevel)
        {
            return aLogLevel <= this.LogLevel;
        }

        /// <inheritdoc/>
        public virtual void logError(string aMessage)
        {
            this.logError(LogLevel.Normal, aMessage);
        }

        /// <inheritdoc/>
        public virtual void logWarning(string aMessage)
        {
            this.logWarning(LogLevel.Normal, aMessage);
        }

        /// <inheritdoc/>
        public virtual void logMessage(string aMessage)
        {
            this.logMessageThreadLock(LogLevel.Normal, aMessage);
        }

        /// <inheritdoc/>
        public virtual void logException(Exception aException)
        {
            this.logException(LogLevel.Normal, aException);
        }

        /// <inheritdoc/>
        public virtual void logException(Exception aException, string aMessage)
        {
            this.logException(LogLevel.Normal, aException, aMessage);
        }
    }
}
