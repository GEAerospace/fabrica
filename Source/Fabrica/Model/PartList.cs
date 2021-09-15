// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace GEAviation.Fabrica.Model 
{
    /// <summary>
    /// This class represents a List of Parts/Part References within
    /// a Blueprint model. This extends a normal <see cref="List{T}"/>
    /// in order to apply additional interfaces for use within the Blueprint Model.
    /// </summary>
    public class PartList : List<IPartDefOrRef>, IPart, ICanHaveTemporaryID, IHasRuntimeType
    {
        /// <summary>
        /// The Runtime Type of this Part.
        /// </summary>
        public ITypeDefOrRef RuntimeType { get; set; }
        
        private Guid mPartID;

        /// <summary>
        /// The ID of the Part. This must be globally unique across
        /// all blueprints within the same model.
        /// </summary>
        public Guid ID 
        {
            get
            {
                return mPartID;
            }
            set
            {
                if( value == Guid.Empty )
                {
                    mPartID        = Guid.NewGuid();
                    HasTemporaryID = true;
                }
                else
                {
                    mPartID        = value;
                    HasTemporaryID = false;
                }
            }
        }

        public string                        Name     { get; set; }
        public IDictionary<string, string> Metadata { get; }

        public PartList()
            : base()
        {
            ID       = Guid.Empty;
            Metadata = new Dictionary<string, string>();
        }

        /// <summary>
        /// Copy constructor. Generates a deep copy of the specified <see cref="PartList"/>.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public PartList(PartList aToCopy, bool aShallow = false)
            : this()
        {
            Name = aToCopy.Name;
            RuntimeType = aToCopy.RuntimeType.createCopy();
            mPartID = aToCopy.mPartID;
            HasTemporaryID = aToCopy.HasTemporaryID;

            foreach(var lMetadata in aToCopy.Metadata)
            {
                Metadata[lMetadata.Key] =lMetadata.Value;
            }

            if(!aShallow)
            { 
                foreach(var lElement in aToCopy)
                {
                    this.Add(lElement.createCopy());
                }
            }
        }

        public bool HasTemporaryID { get; private set; } = true;
    }
}
