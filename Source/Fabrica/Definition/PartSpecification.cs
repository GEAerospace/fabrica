// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace GEAviation.Fabrica.Definition
{
    /// <summary>
    /// This class is used to collect and provide Part information about a
    /// <see cref="Type"/> that is intended to be a Fabrica Part.
    /// </summary>
    public class PartSpecification : IPartSpecification
    {
        /// <summary>
        /// This constant is used to encapsulate flags for "all instance members that are public or non-public".
        /// </summary>
        private const BindingFlags cAllInstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// The name of the Part.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The implementing type of the Part.
        /// </summary>
        public Type PartType { get; private set; }

        /// <summary>
        /// The developer-provided description of the part. May be null/empty.
        /// </summary>
        public string PartDescription { get; private set; }

        /// <summary>
        /// This value indicates if the part the specification represents 
        /// is a Part Locator (true) or is not (false).
        /// </summary>
        public bool IsPartLocator { get; private set; } = false;

        /// <summary>
        /// If <see cref="IsPartLocator"/> is true, this will contain
        /// the scheme that the locator can handle. <see cref="string.Empty"/>
        /// otherwise.
        /// </summary>
        public string PartLocationScheme { get; private set; } = String.Empty;

        /// <summary>
        /// A collection of all of the Properties of the Part Type that are
        /// intended to be loaded by Fabrica.
        /// </summary>
        public IDictionary<string, Type> Properties { get; private set; }

        /// <summary>
        /// A collection of all of the Required Properties of the Part Type that are
        /// intended to be loaded by Fabrica. This will be either the same as, or a subset of, the
        /// <see cref="PartSpecification.Properties"/> collection.
        /// </summary>
        public IDictionary<string, Type> RequiredProperties { get; private set; }

        /// <summary>
        /// A collection of developer-provided descriptions for the Part's Properties.
        /// The keys of this collection will be the same as the keys in the <see cref="PartSpecification.Properties"/>
        /// collection, however this collection may only have a subset of those keys (or none at all).
        /// </summary>
        public IDictionary<string, string> PropertyDescriptions { get; private set; }

        /// <summary>
        /// Returns the default (unnamed) part constructor for the part this specification describes.
        /// </summary>
        public IPartConstructorInfo DefaultConstructor { get; private set; }

        /// <summary>
        /// Returns a dictionary of named part constructors for the part this specification describes.
        /// </summary>
        public IDictionary<string, IPartConstructorInfo> NamedPartConstructors { get; private set; } = new Dictionary<string, IPartConstructorInfo>();

        /// <summary>
        /// Concrete implementation of the <see cref="IPartConstructorInfo"/> interface.
        /// </summary>
        public class PartConstructorInfo : IPartConstructorInfo
        {
            /// <summary>
            /// The constructor to call to use this PartConstructor.
            /// </summary>
            public ConstructorInfo Constructor { get; internal set; }

            /// <summary>
            /// A collection of all of the Features (dependencies) of the Part Type that are
            /// intended to be loaded by Fabrica. This includes both 
            /// </summary>
            public IDictionary<string, Type> Features { get; internal set; } = new Dictionary<string, Type>();

            /// <summary>
            /// A collection of all of the Required Features (dependencies) of the Part Type that are
            /// intended to be loaded by Fabrica. This will be either the same as, or a subset of, the
            /// <see cref="PartConstructorInfo.Features"/> collection.
            /// </summary>
            public IDictionary<string, Type> RequiredFeatures { get; internal set; } = new Dictionary<string, Type>();

            /// <summary>
            /// A collection of developer-provided descriptions for the Part's Features.
            /// The keys of this collection will be the same as the keys in the <see cref="PartConstructorInfo.Features"/>
            /// collection, however this collection may only have a subset of those keys (or none at all).
            /// </summary>
            public IDictionary<string, string> FeatureDescriptions { get; internal set; } = new Dictionary<string, string>();

            /// <summary>
            /// The order that the features must appear in order to call the constructor.
            /// </summary>
            public IList<string> FeatureOrder { get; internal set; } = new List<string>();
        }

        /// <summary>
        /// This method will instantiate an instance of the Part this <see cref="PartSpecification"/> 
        /// represents using the provided Features and Properties. All Required Features and Properties must be
        /// provided.
        /// </summary>
        /// <param name="aFeatures">
        /// A collection of Feature Part instances containing, at a minimum, those Features listed in
        /// the <see cref="PartSpecification.RequiredFeatures"/> collection, and being assignable to
        /// the types of those features. The keys must exactly match those provided in either the
        /// <see cref="PartSpecification.Features"/> or <see cref="PartSpecification.RequiredFeatures"/>
        /// collections.
        /// </param>
        /// <param name="aProperties">
        /// A collection of Property values containing, at a minimum, values for those Properties listed in
        /// the <see cref="PartSpecification.RequiredProperties"/> collection. The keys must exactly match 
        /// those provided in either the <see cref="PartSpecification.Features"/> or
        /// <see cref="PartSpecification.RequiredFeatures"/> collections.
        /// </param>
        /// <exception cref="AggregateException">
        /// Thrown when mutliple exceptions have happened in this method. See <see cref="AggregateException.InnerExceptions"/>
        /// for all exceptions that occurred. This may only contain one Exception if that Exception occurred
        /// in a context where multiple Exceptions may happen.
        /// </exception>
        /// <exception cref="MissingFeatureOrPropertyException">
        /// Thrown if any of the Part's required Features or Properties were not provided a value in the
        /// arguments of this function.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either function argument was null, or if the value for a Part's required feature or 
        /// property was null.
        /// </exception>
        /// <exception cref="TypeMismatchException">
        /// Thrown if the value given for a particular feature or property is incompatible with the
        /// target feature or property.
        /// </exception>
        /// <exception cref="InvalidPartSpecificationException">
        /// Thrown if the Part Specification contains a read-only Property exposed via Fabrica and a value 
        /// for that property was provided as an argument to this function, but that property is read-only.
        /// </exception>
        /// <exception cref="FailedPartInstantiationException">
        /// Thrown if the reflective instantiation of the part fails for uncontrolled reasons. 
        /// The InnerException may contain further information.
        /// </exception>
        /// <returns>
        /// If all required Features were provided, non-null and assignable to those features' type; and
        /// all required Properties were provided, non-null and convertible to those properties' types; a
        /// valid instance of the object will be returned. If any requirement was not met or incompatible,
        /// an <see cref="AggregateException"/> will be thrown with as many relevant inner exceptions as 
        /// could be detected.
        /// </returns>
        public object instantiatePart( IDictionary<string, object> aFeatures, IDictionary<string, object> aProperties )
        {
            return instantiatePart( null, aFeatures, aProperties );
        }

        /// <summary>
        /// This method will instantiate an instance of the Part this <see cref="PartSpecification"/> 
        /// represents using the provided Features and Properties. All Required Features and Properties must be
        /// provided.
        /// </summary>
        /// <param name="aConstructorName">
        /// The name of the Part Constructor to use when instantiating the part. If this value is
        /// null, empty or whitespace, the default part constructor will be used.
        /// </param>
        /// <param name="aFeatures">
        /// A collection of Feature Part instances containing, at a minimum, those Features listed in
        /// the <see cref="PartSpecification.RequiredFeatures"/> collection, and being assignable to
        /// the types of those features. The keys must exactly match those provided in either the
        /// <see cref="PartSpecification.Features"/> or <see cref="PartSpecification.RequiredFeatures"/>
        /// collections.
        /// </param>
        /// <param name="aProperties">
        /// A collection of Property values containing, at a minimum, values for those Properties listed in
        /// the <see cref="PartSpecification.RequiredProperties"/> collection. The keys must exactly match 
        /// those provided in either the <see cref="PartSpecification.Features"/> or
        /// <see cref="PartSpecification.RequiredFeatures"/> collections.
        /// </param>
        /// <exception cref="AggregateException">
        /// Thrown when mutliple exceptions have happened in this method. See <see cref="AggregateException.InnerExceptions"/>
        /// for all exceptions that occurred. This may only contain one Exception if that Exception occurred
        /// in a context where multiple Exceptions may happen.
        /// </exception>
        /// <exception cref="MissingFeatureOrPropertyException">
        /// Thrown if any of the Part's required Features or Properties were not provided a value in the
        /// arguments of this function.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either function argument was null, or if the value for a Part's required feature or 
        /// property was null.
        /// </exception>
        /// <exception cref="TypeMismatchException">
        /// Thrown if the value given for a particular feature or property is incompatible with the
        /// target feature or property.
        /// </exception>
        /// <exception cref="InvalidPartSpecificationException">
        /// Thrown if the Part Specification contains a read-only Property exposed via Fabrica and a value 
        /// for that property was provided as an argument to this function, but that property is read-only.
        /// </exception>
        /// <exception cref="FailedPartInstantiationException">
        /// Thrown if the reflective instantiation of the part fails for uncontrolled reasons. 
        /// The InnerException may contain further information.
        /// </exception>
        /// <returns>
        /// If all required Features were provided, non-null and assignable to those features' type; and
        /// all required Properties were provided, non-null and convertible to those properties' types; a
        /// valid instance of the object will be returned. If any requirement was not met or incompatible,
        /// an <see cref="AggregateException"/> will be thrown with as many relevant inner exceptions as 
        /// could be detected.
        /// </returns>
        public object instantiatePart( string aConstructorName, IDictionary<string, object> aFeatures, IDictionary<string, object> aProperties )
        {
            List<Exception> lExceptions = new List<Exception>();

            var lApprovedFeatureValues = new Dictionary<string, object>();
            var lApprovedPropertyValues = new Dictionary<string, object>();

            if ( this.PartType.IsGenericTypeDefinition )
            {
                // There's a chance that the part type is actually a generic type definition (e.g. IList<T>),
                // not a fully-formed generic type (e.g. IList<string>). Fabrica cannot instantiate
                // or fully analyze the features/properties of a generic type definition.
                throw new InvalidOperationException( $"Cannot instantiate this part specification. The Part type '{PartType.Name}' is a generic definition." );
            }

            IPartConstructorInfo lConstructorInfo = null;

            if ( !string.IsNullOrWhiteSpace( aConstructorName ) )
            {
                if ( !this.NamedPartConstructors.ContainsKey( aConstructorName ) )
                {
                    throw new InvalidOperationException( $"Cannot instantiate this part specification. The Part Constructor '{aConstructorName}' was not found." );
                }

                lConstructorInfo = this.NamedPartConstructors[aConstructorName];
            }
            else
            {
                if ( this.DefaultConstructor == null )
                {
                    throw new InvalidOperationException( $"Cannot instantiate this part specification. Default constructor requested, but doesn't exist." );
                }

                lConstructorInfo = this.DefaultConstructor;
            }

            // Parse/Validate privided Features
            if ( aFeatures == null )
            {
                lExceptions.Add( new ArgumentNullException( nameof( aFeatures ) ) );
            }
            else
            {
                // Check for required features that were not provided
                // a value by the caller.
                var lMissingFeaturesExceptions =
                    lConstructorInfo.RequiredFeatures.Where( aItem => !aFeatures.ContainsKey( aItem.Key ) )
                                    .Select( aItem => MissingFeatureOrPropertyException.createStandard( aItem.Key ) );

                lExceptions.AddRange( lMissingFeaturesExceptions );

                // Validate provided features.
                foreach ( var lFeature in aFeatures )
                {
                    if ( lConstructorInfo.Features.ContainsKey( lFeature.Key ) )
                    {
                        var lFeaturePart = lFeature.Value;
                        var lExpectedType = lConstructorInfo.Features[lFeature.Key];

                        // Null feature values are OK if they are for optional features.
                        if ( lFeaturePart == null && lConstructorInfo.RequiredFeatures.ContainsKey( lFeature.Key ) )
                        {
                            lExceptions.Add( new ArgumentNullException( $"Feature '{lFeature.Key}' value cannot be null." ) );
                        }
                        // Attempt to apply implicit/explicit casting
                        else if ( tryUserDefinedCasting( lFeature.Value, lExpectedType, out var lCastedObject ) )
                        {
                            lApprovedFeatureValues[lFeature.Key] = lCastedObject;
                        }
                        // Attempt to auto-convert a string value to the feature's type.
                        else if ( lFeaturePart is string lFeatureValueString && !lExpectedType.IsInstanceOfType( lFeaturePart ) )
                        {
                            var lConverter = TypeDescriptor.GetConverter( lExpectedType );

                            try
                            {
                                var lConvertedValue = lConverter.ConvertFromString( lFeatureValueString );
                                lApprovedFeatureValues[lFeature.Key] = lConvertedValue;
                            }
                            catch ( Exception )
                            {
                                lExceptions.Add( new TypeMismatchException( $"Could not convert feature '{lFeature.Key}' constant value '{lFeatureValueString}' to type '{lExpectedType.Name}'." ) );
                            }
                        }
                        // Null feature values don't really have a type to check, since it's an object reference,
                        // but if it's non-null, the object has to be assignable to the target feature type.
                        else if ( lFeaturePart != null && !lExpectedType.IsInstanceOfType( lFeaturePart ) )
                        {
                            lExceptions.Add( new TypeMismatchException( lFeature.Key, lExpectedType, lFeaturePart.GetType() ) );
                        }
                        // If everything went great, add it to the "approved" list.
                        else
                        {
                            lApprovedFeatureValues[lFeature.Key] = lFeature.Value;
                        }
                    }
                }
            }

            // Parse/Validate privided Properties
            if ( aProperties == null )
            {
                lExceptions.Add( new ArgumentNullException( nameof( aProperties ) ) );
            }
            else
            {
                var lMissingPropertiesExceptions =
                    RequiredProperties.Where( aItem => !aProperties.ContainsKey( aItem.Key ) )
                                      .Select( aItem => MissingFeatureOrPropertyException.createStandard( aItem.Key ) );

                lExceptions.AddRange( lMissingPropertiesExceptions );

                // Validate provided properties.
                foreach ( var lProperty in aProperties )
                {
                    if ( Properties.ContainsKey( lProperty.Key ) )
                    {
                        var lPropertyValue = lProperty.Value;
                        var lExpectedType = Properties[lProperty.Key];

                        // Null values are OK if they are for optional properties.
                        if ( lPropertyValue == null && RequiredProperties.ContainsKey( lProperty.Key ) )
                        {
                            lExceptions.Add( new ArgumentNullException( $"Property '{lProperty.Key}' value cannot be null." ) );
                        }
                        // Null property values don't really have a type to check, since it's an object reference,
                        // but if it's non-null, the object has to be assignable to the target feature type.
                        else if ( lPropertyValue != null )
                        {
                            if ( lPropertyValue.GetType() == lExpectedType )
                            {
                                lApprovedPropertyValues[lProperty.Key] = lPropertyValue;
                            }
                            // Attempt to apply implicit/explicit casting
                            else if ( tryUserDefinedCasting( lProperty.Value, lExpectedType, out var lCastedObject ) )
                            {
                                lApprovedPropertyValues[lProperty.Key] = lCastedObject;
                            }
                            else if ( lPropertyValue is string lPropertyValueString )
                            {
                                // Type checking here is trickier, since the property mechanism
                                // accepts just "Strings" and hopes that they can be converted to
                                // the expected type.

                                var lConverter = TypeDescriptor.GetConverter( lExpectedType );

                                try
                                {
                                    var lConvertedValue = lConverter.ConvertFromString( lPropertyValueString );
                                    lApprovedPropertyValues[lProperty.Key] = lConvertedValue;
                                }
                                catch ( Exception )
                                {
                                    lExceptions.Add( new TypeMismatchException( $"Could not convert property '{lProperty.Key}' value '{lPropertyValue}' to type '{lExpectedType.Name}'." ) );
                                }
                            }
                        }
                    }
                }
            }

            // Trying to give users as many exceptions at once to reduce debugging.
            // Throwing here to protect the remainder of the method.
            if ( lExceptions.Any() )
            {
                throw new AggregateException( lExceptions );
            }

            // Build the constructor argument list.
            List<object> lConstructorArguments = new List<object>();

            foreach ( var lFeature in lConstructorInfo.FeatureOrder )
            {
                // The null behavior in here is important. It serves as
                // the mechanism to handle optional Features that were not
                // specified by the caller.
                object lFeatureValue = null;

                if ( lApprovedFeatureValues.ContainsKey( lFeature ) )
                {
                    lFeatureValue = lApprovedFeatureValues[lFeature];
                }

                // If the caller didn't provide a value for this Feature
                // in the Feature order, use null instead, as the feature is optional.
                lConstructorArguments.Add( lFeatureValue );
            }

            object lInstantiatedPart = null;

            try
            {
                // Construct the object.
                lInstantiatedPart = lConstructorInfo.Constructor.Invoke( lConstructorArguments.ToArray() );

                // This should be the same as this.PartType, but for safety, it's grabbed
                // from the actual object instance instead.
                Type lFinalPartType = lInstantiatedPart.GetType();

                // Read through the target properties, and attempt to set them.
                foreach ( var lProperty in lApprovedPropertyValues )
                {
                    if ( lFinalPartType.GetProperty( lProperty.Key, cAllInstanceMembers ) is PropertyInfo lPropInfo
                        && lPropInfo.GetSetMethod( true ) is MethodInfo lSetter )
                    {
                        try
                        {
                            lSetter.Invoke( lInstantiatedPart, new[] { lProperty.Value } );
                        }
                        catch ( Exception lException )
                        {
                            // An exception here indicates a problem with calling "set" on the property.
                            lExceptions.Add( lException );
                        }
                    }

                    // It's technically not possible to get into this else case.
                    // I've left it here to indicate that I've thought about what
                    // might happen, but the guards in createPartSpecification() eliminate
                    // this as a possibility. Code is left out for statement coverage purposes.

                    //else
                    //{
                    //    lExceptions.Add( new InvalidPartSpecificationException( $"Part Property '{lProperty.Key}' is read-only or could not be found." ) );
                    //}
                }

                // Any exceptions at this point indicate that the property setting phase has failed,
                // either completely or partially. As a result, instantiation has failed.
                // This prevents callers from receiving incomplete Parts.
                if ( lExceptions.Any() )
                {
                    throw new AggregateException( lExceptions );
                }
            }
            catch ( Exception lException ) when ( !( lException is AggregateException ) )
            {
                // Wrapped throw since this exception likely has more to do with
                // a mistake that Fabrica has made rather than that of the user.
                throw new FailedPartInstantiationException( this.Name, lException );
            }

            if ( lInstantiatedPart is IPropertiesSetNotification lNotification )
            {
                try
                {
                    // Notify the object that its properties are set, if it wants to know
                    // (i.e. has implemented the interface)
                    lNotification.propertiesSet();
                }
                catch ( Exception lException )
                {
                    throw new InvalidOperationException( "Instantiated part raised an exception during 'Property Set Notification'. See InnerException for details.", lException );
                }
            }

            // Object is ready to go!
            return lInstantiatedPart;
        }

        /// <summary>
        /// This method can be used to determine if a particular type is intended to be
        /// a Fabrica Part. This method simply checks the Type for the existence of the
        /// <see cref="PartAttribute"/>.
        /// </summary>
        /// <param name="aPotentialPart">
        /// The Type to check.
        /// </param>
        /// <returns>
        /// True if the Type is intended to be used within the Fabrica Part system,
        /// false otherwise.
        /// </returns>
        public static bool isPart( Type aPotentialPart )
        {
            var lPartAttr = aPotentialPart.GetCustomAttributes( typeof( PartAttribute ), false );
            return lPartAttr.Length > 0;
        }

        /// <summary>
        /// This static method will create a <see cref="PartSpecification"/> from the
        /// specified Type if that Type is a Fabrica Part.
        /// </summary>
        /// <param name="aPotentialPart">
        /// The <see cref="Type"/> of the candidate Fabrica Part.
        /// </param>
        /// <returns>
        /// The <see cref="PartSpecification"/> for the specified Fabrica Part type or
        /// null if the specified type is not a Fabrica Part.
        /// </returns>
        public static IPartSpecification createPartSpecification( Type aPotentialPart )
        {
            // Method to get the specified Attribute
            AttrType getAttribute<AttrType>( Func<Type, bool, object[]> aGetAttributesMethod )
            {
                var lDescAttr = aGetAttributesMethod( typeof( AttrType ), false );
                if ( lDescAttr.Length > 0 )
                {
                    return (AttrType)lDescAttr[0];
                }

                return default( AttrType );
            }

            // Method to get the value of the Description attribute of something,
            // or a nice default.
            string getDescription( Func<Type, bool, object[]> aGetAttributesMethod )
            {
                var lDescAttr = getAttribute<DescriptionAttribute>( aGetAttributesMethod );
                return lDescAttr?.Description;
            }

            if ( aPotentialPart == null )
            {
                throw new ArgumentNullException( nameof( aPotentialPart ) );
            }

            if ( isPart( aPotentialPart ) )
            {
                PartSpecification lSpec = new PartSpecification();

                // Get Part Info
                lSpec.Name = aPotentialPart.FullName;
                lSpec.PartType = aPotentialPart;
                lSpec.PartDescription = getDescription( aPotentialPart.GetCustomAttributes );

                var lPartLocatorAttribute = getAttribute<PartLocatorAttribute>( aPotentialPart.GetCustomAttributes );

                if ( !string.IsNullOrWhiteSpace( lPartLocatorAttribute?.LocatorScheme ) )
                {
                    lSpec.IsPartLocator = true;
                    lSpec.PartLocationScheme = lPartLocatorAttribute.LocatorScheme;
                }

                var lConstructors = aPotentialPart.GetConstructors( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance )
                                                  .Select( aConstructor => new { Constructor = aConstructor, Parameters = aConstructor.GetParameters() } )
                                                  .Where( aConstructor => aConstructor?.Parameters?.Count() >= 0 );

                foreach ( var lConstructor in lConstructors )
                {
                    // The constructor to be used by Fabrica must be public and appropriately
                    // marked with a PartConstructor attribute.
                    if ( getAttribute<PartConstructorAttribute>( lConstructor.Constructor.GetCustomAttributes ) is PartConstructorAttribute lConstructorAttribute )
                    {
                        PartConstructorInfo lNewConstructorInfo = new PartConstructorInfo();

                        // Prepare the collections for a new constructor, in case
                        // a previous iteration of this loop found a partially defined
                        // Part constructor.
                        //lSpec.Features.Clear();
                        //lSpec.FeatureDescriptions.Clear();
                        //lSpec.RequiredFeatures.Clear();
                        //lSpec.mFeatureOrder.Clear();

                        foreach ( var lParameter in lConstructor.Parameters )
                        {
                            var lFeatureAttr = getAttribute<FeatureAttribute>( lParameter.GetCustomAttributes );

                            // Every parameter in the constructor must be marked with a FeatureAttribute in
                            // order to auto-construct it. If any don't have it, discard this constructor.
                            if ( lFeatureAttr == null )
                            {
                                throw new InvalidPartSpecificationException( $"Every parameter of a constructor marked with {nameof( PartConstructorAttribute )} must have a {nameof( FeatureAttribute )}." );
                            }

                            var lDescription = getDescription( lParameter.GetCustomAttributes );
                            var lFeatureType = lParameter.ParameterType;

                            lNewConstructorInfo.FeatureOrder.Add( lFeatureAttr.FeatureName );
                            lNewConstructorInfo.Features[lFeatureAttr.FeatureName] = lFeatureType;

                            if ( !string.IsNullOrWhiteSpace( lDescription ) )
                            {
                                lNewConstructorInfo.FeatureDescriptions[lFeatureAttr.FeatureName] = lDescription;
                            }

                            if ( lFeatureAttr.Required )
                            {
                                lNewConstructorInfo.RequiredFeatures[lFeatureAttr.FeatureName] = lFeatureType;
                            }
                        }

                        // This should also work in the case of a public parameterless constructor.
                        if ( lNewConstructorInfo.FeatureOrder.Count == lConstructor.Parameters.Length )
                        {
                            lNewConstructorInfo.Constructor = lConstructor.Constructor;
                        }

                        if ( !string.IsNullOrWhiteSpace( lConstructorAttribute.Name ) )
                        {
                            // Named constructor
                            if ( lSpec.NamedPartConstructors.ContainsKey( lConstructorAttribute.Name ) )
                            {
                                throw new InvalidPartSpecificationException( $"Part '{aPotentialPart.Name}' has multiple part constructors with the name '{lConstructorAttribute.Name}'." );
                            }

                            lSpec.NamedPartConstructors[lConstructorAttribute.Name] = lNewConstructorInfo;
                        }
                        else
                        {
                            // Default no-name
                            if ( lSpec.DefaultConstructor != null )
                            {
                                throw new InvalidPartSpecificationException( $"Part '{aPotentialPart.Name}' has multiple default (nameless) part constructors. Only one is allowed." );
                            }

                            lSpec.DefaultConstructor = lNewConstructorInfo;
                        }
                    }
                }

                if ( lSpec.DefaultConstructor == null && lSpec.NamedPartConstructors.Count == 0 )
                {
                    throw new InvalidPartSpecificationException( $"Part '{aPotentialPart.Name}' requires at least one public constructor marked with a PartConstructorAttribute, with each parameter marked with a FeatureAttribute." );
                }

                // Read properties.
                var lProperties = aPotentialPart.GetProperties( cAllInstanceMembers );

                foreach ( var lProperty in lProperties )
                {
                    if ( getAttribute<PropertyAttribute>( lProperty.GetCustomAttributes ) is PropertyAttribute lPropertyAttribute )
                    {
                        if ( !lProperty.CanWrite )
                        {
                            throw new InvalidPartSpecificationException( $"Part property '{lProperty.Name}' must have a set accessor in order to be used as a Part Property." );
                        }

                        lSpec.Properties[lProperty.Name] = lProperty.PropertyType;

                        var lDescription = getDescription( lProperty.GetCustomAttributes );

                        if ( !string.IsNullOrWhiteSpace( lDescription ) )
                        {
                            lSpec.PropertyDescriptions[lProperty.Name] = lDescription;
                        }

                        if ( lPropertyAttribute.Required )
                        {
                            lSpec.RequiredProperties[lProperty.Name] = lProperty.PropertyType;
                        }
                    }
                }

                return lSpec;
            }

            throw new InvalidPartSpecificationException( $"Part '{aPotentialPart.Name}' is not marked with PartAttribute." );
        }

        // Eliminating public parameterless constructor.
        private PartSpecification()
        {
            Properties = new Dictionary<string, Type>();
            RequiredProperties = new Dictionary<string, Type>();
            PropertyDescriptions = new Dictionary<string, string>();
        }

        /// <summary>
        /// This method can be used to analyze an assembly for Types that are implemented in
        /// accordance with Fabrica Parts.
        /// </summary>
        /// <param name="aOwningAssemblies">
        /// The collection of assemblies to search.
        /// </param>
        /// <param name="aPartDiscoveryExceptions">
        /// An <see cref="AggregateException"/> containing any exceptions that occurred during the
        /// part discovery process.
        /// </param>
        /// <returns>
        /// A collection of <see cref="IPartSpecification"/> objects representing all valid parts
        /// found within the specified assemblies.
        /// </returns>
        public static IEnumerable<IPartSpecification> getSpecifications( IEnumerable<Assembly> aOwningAssemblies, out AggregateException aPartDiscoveryExceptions )
        {
            aPartDiscoveryExceptions = null;

            if ( aOwningAssemblies == null )
            {
                throw new ArgumentNullException( nameof( aOwningAssemblies ) );
            }

            List<IPartSpecification> lSpecs = new List<IPartSpecification>();
            List<Exception> lExceptions = new List<Exception>();

            foreach ( var lAssembly in aOwningAssemblies )
            {
                if ( lAssembly != null )
                {
                    var lTypes = lAssembly.GetTypes();

                    foreach ( var lType in lTypes )
                    {
                        try
                        {
                            if ( isPart( lType ) )
                            {
                                lSpecs.Add( createPartSpecification( lType ) );
                            }
                        }
                        catch ( Exception lException )
                        {
                            lExceptions.Add( lException );
                        }
                    }
                }
            }

            if ( lExceptions.Count > 0 )
            {
                aPartDiscoveryExceptions = new AggregateException( "A part specification could not be generated for one or more parts.", lExceptions );
            }

            return lSpecs;
        }

        /// <summary>
        /// This method can be used to analyze an assembly for Types that are implemented in
        /// accordance with Fabrica Parts.
        /// </summary>
        /// <param name="aOwningAssembly">
        /// The assembly to search.
        /// </param>
        /// <param name="aPartDiscoveryExceptions">
        /// An <see cref="AggregateException"/> containing any exceptions that occurred during the
        /// part discovery process.
        /// </param>
        /// <returns>
        /// A collection of <see cref="IPartSpecification"/> objects representing all valid parts
        /// found within the assembly.
        /// </returns>
        public static IEnumerable<IPartSpecification> getSpecifications( Assembly aOwningAssembly, out AggregateException aPartDiscoveryExceptions )
        {
            return getSpecifications( new List<Assembly>() { aOwningAssembly }, out aPartDiscoveryExceptions );
        }

        /// <summary>
        /// Attempts to cast an object to the specified type using implemented cast
        /// overloads (i.e. implicit/explicit cast operator overloads). If no compatible
        /// overload exists on either type, the method will return false.
        /// </summary>
        /// <param name="aObject">
        /// The object to be casted.
        /// </param>
        /// <param name="aExpectedType">
        /// The type to cast to.
        /// </param>
        /// <param name="aAsExpected">
        /// If successful, the casted object.
        /// </param>
        /// <returns>
        /// True if the cast was successful, false otherwise.
        /// </returns>
        private static bool tryUserDefinedCasting( object aObject, Type aExpectedType, out object aAsExpected )
        {
            // Find a cast method from aObject's Type.
            var lToMethod = aObject.GetType().GetMethods( BindingFlags.Public | BindingFlags.Static )
                             .Where( aMethod =>
                             {
                                 ParameterInfo lParam = aMethod.GetParameters().FirstOrDefault();
                                 return lParam != null
                                        && lParam.ParameterType == aObject.GetType()
                                        && ( aMethod.Name == "op_Implicit" || aMethod.Name == "op_Explicit" )
                                        && aExpectedType.IsAssignableFrom( aMethod.ReturnType );
                             } ).FirstOrDefault();

            // Find a cast method from aExpectedType.
            var lFromMethod = aExpectedType.GetMethods( BindingFlags.Public | BindingFlags.Static )
                             .Where( aMethod =>
                             {
                                 ParameterInfo lParam = aMethod.GetParameters().FirstOrDefault();
                                 return lParam != null
                                        && lParam.ParameterType == aObject.GetType()
                                        && ( aMethod.Name == "op_Implicit" || aMethod.Name == "op_Explicit" )
                                        && aExpectedType.IsAssignableFrom( aMethod.ReturnType );
                             } ).FirstOrDefault();

            var lMethod = lToMethod ?? lFromMethod;
            if ( lMethod != null )
            {
                aAsExpected = lMethod.Invoke( null, new object[] { aObject } );
                return true;
            }

            aAsExpected = null;
            return false;
        }
    }
}