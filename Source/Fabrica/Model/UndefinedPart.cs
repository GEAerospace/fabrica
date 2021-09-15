// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace GEAviation.Fabrica.Model 
{
    /// <summary>
    /// This represents an undefined part within the Blueprint Model. This
    /// allows users/tools to include incomplete parts within their model,
    /// generate references to that part and do analyses that depend on
    /// that part, while deferring full definition for later.
    /// </summary>
    public class UndefinedPart : IPart
    {
        /// <summary>
        /// The ID of the Part. This must be globally unique across
        /// all blueprints within the same model.
        /// </summary>
        public Guid ID { get; set; }
        
        /// <summary>
        /// The user-friendly name of the Part. This is optional.
        /// This must be unique within a single Blueprint.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A collection of additional metadata for the Part. This
        /// is tool/user-defined data for consumption outside of
        /// Fabrica. 
        /// </summary>
        public IDictionary<string, string> Metadata { get; }

        /// <summary>
        /// Constructs a new <see cref="UndefinedPart"/> for use within the
        /// Blueprint Model.
        /// </summary>
        public UndefinedPart()
        {
            ID = Guid.Empty;
            Metadata = new Dictionary<string, string>();
        }

        /// <summary>
        /// Copy constructor. Generates a deep copy of the specified <see cref="UndefinedPart"/>.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public UndefinedPart(UndefinedPart aToCopy)
            : this()
        {
            ID = aToCopy.ID;
            Name = aToCopy.Name;
            foreach(var lMetadata in aToCopy.Metadata)
            {
                Metadata[lMetadata.Key] = lMetadata.Value;
            }
        }
    }
}
