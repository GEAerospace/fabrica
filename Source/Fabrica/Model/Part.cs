// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace GEAviation.Fabrica.Model 
{
    /// <summary>
    /// Represents a full Part within the Fabrica Blueprint Model.
    /// </summary>
    public class Part : IPart, IHasLocationScheme, ICanHaveTemporaryID, IHasRuntimeType
    {
        // TODO > TypeAliases should be phased out in favor of just
        //        simply expecting a TypeDefinition. The file format
        //        should define a alias system, if it wants. The model
        //        shouldn't care.

        // TODO > Runtime Type should be made a TypeDefinition instead of
        //        a ITypeDefOrRef.

        /// <summary>
        /// The Runtime Type of this Part.
        /// </summary>
        public ITypeDefOrRef RuntimeType { get; set; }

        /// <summary>
        /// If this Part is a <see cref="Definition.IPartLocator"/>, this
        /// property represents the Uri scheme that it handles.
        /// </summary>
        public string LocationScheme { get; set; } = string.Empty;

        /// <summary>
        /// If this part allows for additional PartConstructors, this 
        /// property specifies the name of which Constructor to use.
        /// </summary>
        public string Constructor { get; set; } = string.Empty;

        /// <summary>
        /// The collection of properties and their values defined in the
        /// model. In order for this to be used in constructing an actual
        /// Part Instance, all of the required properties of that Part Specification
        /// must exist in this collection and have compatible values.
        /// </summary>
        public IDictionary<string, IPropertyValueOrSlot> Properties { get; }
        
        /// <summary>
        /// The collection of features and their values defined in the
        /// model. In order for this to be used in constructing an actual
        /// Part Instance, all of the required features of that Part Specification
        /// must exist in this collection and have compatible values.
        /// </summary>
        public IDictionary<string, IPartDefOrRef> Features { get; }

        /// <summary>
        /// Constructs a new Part object for use in the Blueprint model.
        /// </summary>
        public Part()
        {
            ID = Guid.Empty;
            Metadata = new Dictionary<string, string>();
            Properties = new Dictionary<string, IPropertyValueOrSlot>();
            Features = new Dictionary<string, IPartDefOrRef>();
        }

        /// <summary>
        /// Copy constructor. Generates a deep copy of the specified <see cref="Part"/>.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public Part(Part aToCopy, bool aShallow = false)
            : this()
        {
            mPartID = aToCopy.mPartID;
            Name = aToCopy.Name;
            HasTemporaryID = aToCopy.HasTemporaryID;
            Constructor = aToCopy.Constructor;
            RuntimeType = aToCopy.RuntimeType.createCopy();
            LocationScheme = aToCopy.LocationScheme;

            if(!aShallow)
            { 
                foreach(var lFeature in aToCopy.Features)
                {
                    Features[lFeature.Key] = lFeature.Value.createCopy();
                }

                foreach(var lProperty in aToCopy.Properties)
                {
                    Properties[lProperty.Key] = lProperty.Value.createCopy();
                }
            }

            foreach(var lMetadata in aToCopy.Metadata)
            {
                Metadata[lMetadata.Key] = lMetadata.Value;
            }
        }

        /// <summary>
        /// When true, indicates that the Part's ID is a temporary ID and should
        /// not be persisted when saving Part data.
        /// </summary>
        public bool HasTemporaryID { get; private set; } = true;

        private Guid mPartID;

        /// <summary>
        /// The ID of the Part. This must be globally unique across
        /// all blueprints within the same model.
        /// If set to <see cref="Guid.Empty"/>, the Part will be given a temporary
        /// ID that should not be persisted to save files (and will cause
        /// <see cref="ICanHaveTemporaryID.HasTemporaryID"/> to be set to true.
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
                    mPartID = Guid.NewGuid();
                    HasTemporaryID = true;
                }
                else
                {
                    mPartID = value;
                    HasTemporaryID = false;
                }
            }
        }

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
    }

    public interface IPropertyValueOrSlot
    { }

    public class PropertyValue : IPropertyValueOrSlot
    {
        public string Value { get; set; } = String.Empty;
        public Uri ValueUri { get; set; } = null;

        public PropertyValue() { }

        public PropertyValue(PropertyValue aToCopy)
        {
            Value = aToCopy.Value;
            ValueUri = aToCopy.ValueUri;
        }
    }

    public class PropertySlot : IPropertyValueOrSlot
    {
        public string SlotName { get; set; }
        
        public PropertySlot() { }

        public PropertySlot(PropertySlot aToCopy)
        {
            SlotName = aToCopy.SlotName;
        }
    }

    public class FeatureSlot : IPartDefOrRef
    {
        public string SlotName { get; set; }

        public FeatureSlot() { }

        public FeatureSlot(FeatureSlot aToCopy)
        {
            SlotName = aToCopy.SlotName;
        }
    }
}
