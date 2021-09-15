// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using GEAviation.Fabrica.Utility;

namespace GEAviation.Fabrica.Definition
{
    /// <summary>
    /// This attribute is used to indicate to Fabrica that the marked class
    /// is intended to function as a Fabrica Part. In order for Fabrica
    /// to find and instantiate a Part, this attribute must be used
    /// and a public constructor must exist with each parameter representing
    /// a Feature Part (sub part), marked with a <see cref="FeatureAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PartAttribute : Attribute
    { }

    /// <summary>
    /// This attribute is used to indicate to Fabrica that the marked class
    /// is intended to function as a Fabrica Part Locator. In order for Fabrica
    /// to treat a Part as a Part Locator, this attribute must be used
    /// and the marked class must also implement the <see cref="IPartLocator"/>
    /// interface.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, Inherited = false )]
    public class PartLocatorAttribute : Attribute
    {
        /// <summary>
        /// The scheme portion of the URIs that this Part Locator can handle.
        /// </summary>
        public string LocatorScheme { get; }

        /// <summary>
        /// Creates a new <see cref="PartLocatorAttribute"/>.
        /// </summary>
        /// <param name="aLocatorScheme">
        /// The scheme portion of the URIs that this Part Locator can handle
        /// (e.g. everything before the first colon of the URI).
        /// </param>
        public PartLocatorAttribute( string aLocatorScheme )
        {
            LocatorScheme = aLocatorScheme;
        }
    }

    /// <summary>
    /// This attribute is used to indicate to Fabrica that the marked constructor
    /// is intended to function as the dependency injection constructor. Only one
    /// public constructor should be marked with this attribute, and each parameter
    /// must be marked with an appropriate <see cref="FeatureAttribute"/>.
    /// </summary>
    [AttributeUsage( AttributeTargets.Constructor, Inherited = false )]
    public class PartConstructorAttribute : Attribute
    {
        /// <summary>
        /// The name of the constructor.
        /// </summary>
        public string Name { get; private set; } = String.Empty;
        
        /// <summary>
        /// Creates a <see cref="PartConstructorAttribute"/> for the default (nameless)
        /// constructor of a part. There can only be one default part constructor
        /// in a part.
        /// </summary>
        public PartConstructorAttribute() {}

        /// <summary>
        /// Creates a <see cref="PartConstructorAttribute"/> for a non-default (named)
        /// constructor of a part. 
        /// </summary>
        /// <param name="aName">
        /// The name of the constructor.
        /// </param>
        public PartConstructorAttribute( string aName )
        {
            Name = aName;
        }
    }

    /// <summary>
    /// This attribute is used to provide a user-facing description of a Part,
    /// Feature or Property. It is intended for use in Designers or other applications
    /// that interact with Fabrica Parts.
    /// </summary>
    [AttributeUsage( AttributeTargets.All, Inherited = false )]
    public class DescriptionAttribute : Attribute
    {
        /// <summary>
        /// The description string provided with the attribute.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Constructs a DescriptionAttribute with the specified Description.
        /// </summary>
        /// <param name="aDescription">
        /// The description text to contain within this attribute.
        /// </param>
        public DescriptionAttribute( string aDescription )
        {
            Description = aDescription;
        }
    }

    /// <summary>
    /// This attribute is used to mark the parameters of a public constructor
    /// of the Part that will be used to instantiate the Part. All parameters
    /// of that constructor must have this attribute.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Parameter )]
    public class FeatureAttribute : Attribute
    {
        /// <summary>
        /// The name of the feature this attribute applies to.
        /// </summary>
        public string FeatureName { get; }

        /// <summary>
        /// A value indicating whether or not the marked feature is a 
        /// required feature. Required features must be provided during
        /// part instantiation or blueprint assembly, otherwise those processes
        /// will fail.
        /// </summary>
        public bool Required { get; set; } = true;

        /// <summary>
        /// Constructs a FeatureAttribute with the specified Feature.
        /// </summary>
        /// <param name="aFeatureName">
        /// The name of the Feature to give the constructor argument that this
        /// attribute is applied to.
        /// </param>
        public FeatureAttribute( string aFeatureName )
        {
            FeatureName = aFeatureName;
        }
    }

    /// <summary>
    /// This attribute is used to mark the public, writable, Properties of a 
    /// Part that is intended to be filled in by Fabrica during Part construction.
    /// These properties may be public or non-public, but may not be static.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Parameter )]
    public class PropertyAttribute : Attribute
    {
        /// <summary>
        /// A value indicating whether or not the marked property is a 
        /// required property. Required properties must be provided during
        /// part instantiation or blueprint assembly, otherwise those processes
        /// will fail.
        /// </summary>
        public bool Required { get; set; } = false;
    }
}
