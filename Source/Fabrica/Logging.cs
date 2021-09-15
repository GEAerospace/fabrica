// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using GEAviation.Fabrica.Utility;

namespace GEAviation.Fabrica 
{
    /// <summary>
    /// Used to contain the global Logger for the Fabrica library.
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// Implementation of a no-operation <see cref="ILogger"/>, to be used
        /// as a default logger in the library.
        /// </summary>
        private class NullLogger : ILogger
        {
            /// <inheritdoc/>
            public void logError( string aMessage ) { }

            /// <inheritdoc/>
            public void logError( string aFormattedMessage, params object[] aObjects ) { }

            /// <inheritdoc/>
            public void logWarning( string aMessage ) { }

            /// <inheritdoc/>
            public void logWarning( string aFormattedMessage, params object[] aObjects ) { }

            /// <inheritdoc/>
            public void logMessage( string aMessage ) { }

            /// <inheritdoc/>
            public void logMessage( string aFormattedMessage, params object[] aObjects ) { }

            /// <inheritdoc/>
            public void logException( Exception aException ) { }

            /// <inheritdoc/>
            public void logException( Exception aException, string aMessage ) { }

            /// <inheritdoc/>
            public void logException( Exception aException, string aFormattedMessage, params object[] aObjects ) { }

            /// <inheritdoc/>
            public void logError( LogLevel aLogLevel, string aMessage ) { }

            /// <inheritdoc/>
            public void logError( LogLevel aLogLevel, string aFormattedMessage, params object[] aObjects ) { }

            /// <inheritdoc/>
            public void logWarning( LogLevel aLogLevel, string aMessage ) { }

            /// <inheritdoc/>
            public void logWarning( LogLevel aLogLevel, string aFormattedMessage, params object[] aObjects ) { }

            /// <inheritdoc/>
            public void logMessage( LogLevel aLogLevel, string aMessage ) { }

            /// <inheritdoc/>
            public void logMessage( LogLevel aLogLevel, string aFormattedMessage, params object[] aObjects ) { }

            /// <inheritdoc/>
            public void logException( LogLevel aLogLevel, Exception aException ) { }

            /// <inheritdoc/>
            public void logException( LogLevel aLogLevel, Exception aException, string aMessage ) { }

            /// <inheritdoc/>
            public void logException( LogLevel aLogLevel, Exception aException, string aFormattedMessage, params object[] aObjects ) { }

            /// <inheritdoc/>
            public LogLevel LogLevel { get; set; }
        }

        private readonly static ILogger mNullLogger = new NullLogger();
        private static ILogger mLogger;

        /// <summary>
        /// The global logger for Fabrica. Defaults to <see cref="NullLogger"/>
        /// until set by another consuming library/application.
        /// Setting this to null will revert it back to the <see cref="NullLogger"/>.
        /// </summary>
        public static ILogger Logger {
            get
            {
                if( mLogger == null )
                {
                    mLogger = mNullLogger;
                }
                return mLogger;
            }
            set
            {
                if( value != null )
                {
                    mLogger = value;
                    return;
                }

                mLogger = mNullLogger;
            }
        }
    }
}
