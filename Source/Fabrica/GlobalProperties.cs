// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using GEAviation.Fabrica.Extensibility;

namespace GEAviation.Fabrica
{
    /// <summary>
    /// Singleton class for attached properties.
    /// </summary>
    public static class GlobalProperties
    {
        /// <summary>
        /// Attached property for attaching metadata objects to part instances for use
        /// by consumers of those parts.
        /// </summary>
        public static AttachedProperty<IDictionary<string, string>> PartMetadata { get; } =
            new AttachedProperty<IDictionary<string, string>>();

        /// <summary>
        /// Attached property for attaching File/Line location information to objects
        /// in the Fabrica model so that errors can be reported and specifically located within a file.
        /// </summary>
        public static AttachedProperty<string> FileLocationInfo { get; } =
            new AttachedProperty<string>();
    }
}
