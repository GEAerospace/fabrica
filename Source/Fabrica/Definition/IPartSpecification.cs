// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace GEAviation.Fabrica.Definition 
{
    /// <summary>
    /// This interface represents the information necessary to fully describe
    /// a class that implements a Fabrica Part.
    /// </summary>
    public interface IPartSpecification 
    {
        /// <summary>
        /// The name of the Part.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The implementing type of the Part.
        /// </summary>
        Type PartType { get; }

        /// <summary>
        /// The developer-provided description of the part. May be null/empty.
        /// </summary>
        string PartDescription { get; }

        /// <summary>
        /// This value indicates if the part the specification represents 
        /// is a Part Locator (true) or is not (false).
        /// </summary>
        bool IsPartLocator { get; }

        /// <summary>
        /// If <see cref="IsPartLocator"/> is true, this will contain
        /// the scheme that the locator can handle. <see cref="string.Empty"/>
        /// otherwise.
        /// </summary>
        string PartLocationScheme { get; }

        /// <summary>
        /// A collection of all of the Properties of the Part Type that are
        /// intended to be loaded by Fabrica.
        /// </summary>
        IDictionary<string, Type> Properties { get; }

        /// <summary>
        /// A collection of all of the Required Properties of the Part Type that are
        /// intended to be loaded by Fabrica. This will be either the same as, or a subset of, the
        /// <see cref="PartSpecification.Properties"/> collection.
        /// </summary>
        IDictionary<string, Type> RequiredProperties { get; }

        /// <summary>
        /// A collection of developer-provided descriptions for the Part's Properties.
        /// The keys of this collection will be the same as the keys in the <see cref="PartSpecification.Properties"/>
        /// collection, however this collection may only have a subset of those keys (or none at all).
        /// </summary>
        IDictionary<string, string> PropertyDescriptions { get; }

        /// <summary>
        /// Provides information about declared default Part Constructors. If this is null,
        /// a default constructor was not provided.
        /// </summary>
        IPartConstructorInfo DefaultConstructor { get; }

        /// <summary>
        /// Provides information about all named Part Constructors. If this is empty,
        /// no named Part Constructors were provided.
        /// </summary>
        IDictionary<string, IPartConstructorInfo> NamedPartConstructors { get; }

        /// <summary>
        /// When implemented in a class, this method will instantiate an instance of the Part this <see cref="PartSpecification"/> 
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
        object instantiatePart(IDictionary<string,object> aFeatures, IDictionary<string,object> aProperties);

        /// <summary>
        /// When implemented in a class, this method will instantiate an instance of the Part this <see cref="PartSpecification"/> 
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
        object instantiatePart( string aConstructorName, IDictionary<string, object> aFeatures, IDictionary<string, object> aProperties );
    }
}