// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GEAviation.Fabrica.Model.IO 
{
    /// <summary>
    /// This struct is used to record an error that occurred while reading
    /// a Blueprint/Blueprint List from an XML file.
    /// </summary>
    public struct BlueprintIOError
    {
        /// <summary>
        /// Get the severity of the reader error.
        /// </summary>
        public BlueprintProblemSeverity Severity { get; }

        /// <summary>
        /// Get the message of the reader error.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Creates a new <see cref="BlueprintIOError"/> with the specified
        /// severity and message.
        /// </summary>
        /// <param name="aSeverity">
        /// The severity of the error.
        /// </param>
        /// <param name="aMessage">
        /// The message explaining the error.
        /// </param>
        public BlueprintIOError( BlueprintProblemSeverity aSeverity, string aMessage )
        {
            Severity = aSeverity;
            Message = aMessage;
        }
    }
}