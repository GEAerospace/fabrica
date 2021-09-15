// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GEAviation.Fabrica.Utility
{
    /// <summary>
    /// The detail level to use when logging.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Incredibly detailed logging. Should include everything.
        /// </summary>
        Diagnostic = 4,

        /// <summary>
        /// Detailed logging. Should include most information.
        /// </summary>
        Detailed = 3,

        /// <summary>
        /// Normal logging. Should only include general information.
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Minimal logging. Should only include high-level details and important things.
        /// </summary>
        Minimal = 1,

        /// <summary>
        /// Minimal logging. Should only include really high-level details and critical things.
        /// </summary>
        Quiet = 0
    }
}
