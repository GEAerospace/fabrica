// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace GEAviation.Fabrica.Model 
{
    /// <summary>
    /// This Part Model element represents parts that will be provided externally
    /// rather than assembled within the blueprint. As a result, it will always be
    /// a valid dependency when computing the dependency graph.
    /// </summary>
    public class ExternalPart : IPart, IHasLocationScheme
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
        public ExternalPart()
        {
            ID = Guid.Empty;
            Metadata = new Dictionary<string, string>();
        }

        /// <summary>
        /// If this Part is a <see cref="Definition.IPartLocator"/>, this
        /// property represents the Uri scheme that it handles.
        /// </summary>
        public string LocationScheme { get; set; }

        /// <summary>
        /// Copy constructor. Generates a deep copy of the specified <see cref="Blueprint"/>.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public ExternalPart(ExternalPart aToCopy)
            : this()
        {
            ID = aToCopy.ID;
            Name = aToCopy.Name;
            foreach(var lMetadata in aToCopy.Metadata)
            {
                Metadata[lMetadata.Key] =lMetadata.Value;
            }
        }
    }
}