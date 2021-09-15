// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace GEAviation.Fabrica.Model 
{
    /// <summary>
    /// This interface represents anything that resembles a Part definition.
    /// </summary>
    public interface IPart : IPartDefOrRef
    {
        /// <summary>
        /// The ID of the Part. This must be globally unique across
        /// all blueprints within the same model.
        /// </summary>
        Guid ID { get; set; }

        /// <summary>
        /// The user-friendly name of the Part. This is optional.
        /// This must be unique within a single Blueprint.
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// A collection of additional metadata for the Part. This
        /// is tool/user-defined data for consumption outside of
        /// Fabrica. 
        /// </summary>
        IDictionary<string, string> Metadata { get; }
    }
}
