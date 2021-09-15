// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GEAviation.Fabrica.Model
{
    /// <summary>
    /// The class holds extensions methods used to help create deep copies
    /// of Blueprint Model structures. 
    /// </summary>
    /// <remarks>
    /// This is primarily meant for interface types, since the concrete classes 
    /// are utilizing the copy constructor pattern. Since interfaces cannot have
    /// copy constructors, these extensions are provided instead, allowing consumers
    /// to copy the object stored in an interface reference without having to
    /// analyze the underlying concrete type.
    /// pattern. 
    /// </remarks>
    public static class ModelCopyExtensions
    {
        /// <summary>
        /// Copy method. Allows for "copying" a <see cref="ITypeDefOrRef"/> without having to know
        /// the underlying concrete type. Utilizes those classes' copy constructors.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public static ITypeDefOrRef createCopy(this ITypeDefOrRef aToCopy, bool aShallow = false)
        {
            switch(aToCopy)
            {
                case CompositeTypeRef lCompositeTypeRef:
                    return new CompositeTypeRef(lCompositeTypeRef);

                case TypeAlias lTypeAlias:
                    return new TypeAlias(lTypeAlias, aShallow);

                case TypeDefinition lTypeDefinition:
                    return new TypeDefinition(lTypeDefinition, aShallow);
            }

            return null;
        }

        /// <summary>
        /// Copy method. Allows for "copying" a <see cref="IPart"/> without having to know
        /// the underlying concrete type. Utilizes those classes' copy constructors.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public static IPart createCopy(this IPart aToCopy, bool aShallow = false)
        {
            switch(aToCopy)
            {
                case Part lPart:
                    return new Part(lPart, aShallow);

                case ExternalPart lExternalPart:
                    return new ExternalPart(lExternalPart);

                case UndefinedPart lUndefinedPart:
                    return new UndefinedPart(lUndefinedPart);

                case PartList lPartList:
                    return new PartList(lPartList, aShallow);

                case PartDictionary lPartDictionary:
                    return new PartDictionary(lPartDictionary, aShallow);
            }

            return null;
        }

        /// <summary>
        /// Copy method. Allows for "copying" a <see cref="IPartDefOrRef"/> without having to know
        /// the underlying concrete type. Utilizes those classes' copy constructors.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public static IPartDefOrRef createCopy(this IPartDefOrRef aToCopy, bool aShallow = false)
        {
            switch(aToCopy)
            {
                case NamedPartRef lNameRef:
                    return new NamedPartRef(lNameRef);

                case UriPartRef lUriRef:
                    return new UriPartRef(lUriRef);

                case IDPartRef lIdRef:
                    return new IDPartRef(lIdRef);

                case ConstantValue lConstant:
                    return new ConstantValue(lConstant);

                case FeatureSlot lFeatureSlot:
                    return new FeatureSlot(lFeatureSlot);

                case CompositePartDef lCompositeRef:
                    return new CompositePartDef(lCompositeRef, aShallow);

                case IPart lPart:
                    return lPart.createCopy(aShallow);
            }

            return null;
        }

        /// <summary>
        /// Copy method. Allows for "copying" a <see cref="IPropertyValueOrSlot"/> without having to know
        /// the underlying concrete type. Utilizes those classes' copy constructors.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public static IPropertyValueOrSlot createCopy(this IPropertyValueOrSlot aToCopy)
        {
            switch(aToCopy)
            {
                case PropertyValue lPropValue:
                    return new PropertyValue(lPropValue);

                case PropertySlot lPropSlot:
                    return new PropertySlot(lPropSlot);
            }

            return null;
        }
    }
}
