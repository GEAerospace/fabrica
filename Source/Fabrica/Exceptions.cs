// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using GEAviation.Fabrica.Model;

namespace GEAviation.Fabrica
{
    /// <summary>
    /// The base class for all Fabrica-specific exceptions.
    /// </summary>
    public abstract class FabricaException : Exception
    {
        /// <summary>
        /// Constructs a new <see cref="FabricaException"/>.
        /// </summary>
        /// <param name="aMessage">
        /// A message indicating why the <see cref="FabricaException"/> ocurred.
        /// </param>
        internal FabricaException( string aMessage )
            : base( aMessage ) { }

        /// <summary>
        /// Constructs a new <see cref="FabricaException"/>.
        /// </summary>
        /// <param name="aMessage">
        /// A message indicating why the <see cref="FabricaException"/> ocurred.
        /// </param>
        /// <param name="aInnerException">
        /// The underlying <see cref="Exception"/> that caused this exception to be thrown.
        /// </param>
        internal FabricaException( string aMessage, Exception aInnerException )
            : base( aMessage, aInnerException ) { }

        /// <summary>
        /// Constructs a new <see cref="FabricaException"/>.
        /// </summary>
        internal FabricaException() {}
    }

    /// <summary>
    /// This <see cref="FabricaException"/> is thrown when an attempt to
    /// instantiate a part occurs, but not all of the required features and properties
    /// are provided by the caller.
    /// </summary>
    public sealed class MissingFeatureOrPropertyException : FabricaException
    {
        /// <summary>
        /// Constructs a new <see cref="MissingFeatureOrPropertyException"/>.
        /// </summary>
        /// <param name="aMessage">
        /// A message indicating why the <see cref="MissingFeatureOrPropertyException"/> ocurred.
        /// </param>
        internal MissingFeatureOrPropertyException( string aMessage )
            : base( aMessage )
        {}

        /// <summary>
        /// Constructs a new <see cref="MissingFeatureOrPropertyException"/>.
        /// </summary>
        /// <param name="aFeatureOrPropertyName">
        /// The name of the feature or property that was missing.
        /// </param>
        internal static MissingFeatureOrPropertyException createStandard( string aFeatureOrPropertyName ) 
            => new MissingFeatureOrPropertyException( $"Feature/Property '{aFeatureOrPropertyName}' was not provided." );
    }

    /// <summary>
    /// This <see cref="FabricaException"/> is thrown when an attempt is made to
    /// assemble the parts of a <see cref="PartContainer"/> and not all of the
    /// declared external parts are provided by the caller.
    /// </summary>
    public sealed class MissingExternalPartException : FabricaException
    {
        /// <summary>
        /// The External Part reference that could not be found.
        /// </summary>
        public ExternalPart MissingPart { get; }

        /// <summary>
        /// Constructs a new <see cref="MissingExternalPartException"/>.
        /// </summary>
        internal MissingExternalPartException( ExternalPart aMissingPart )
            : base( $"External part not provided for [ID: {aMissingPart?.ID.ToString()}], [Name: {aMissingPart?.Name}], [Scheme: {aMissingPart?.LocationScheme}]." )
        {
            MissingPart = aMissingPart;
        }

        /// <summary>
        /// Constructs a new <see cref="MissingExternalPartException"/>.
        /// </summary>
        internal MissingExternalPartException( ExternalPart aMissingPart, string aLocationInfo )
            : base( $"External part not provided for [ID: {aMissingPart?.ID.ToString()}], [Name: {aMissingPart?.Name}], [Scheme: {aMissingPart?.LocationScheme}] at '{aLocationInfo}'." )
        {
            MissingPart = aMissingPart;
        }
    }

    /// <summary>
    /// This <see cref="FabricaException"/> is thrown when an attempt to create
    /// a <see cref="InvalidPartSpecificationException"/> fails because the class
    /// marked with the <see cref="Definition.PartAttribute"/>  is incorrectly 
    /// formed or because usage of an existing <see cref="Definition.PartSpecification"/>
    /// was erroneous due to the structure of the Part.
    /// </summary>
    public sealed class InvalidPartSpecificationException : FabricaException
    {
        /// <summary>
        /// Constructs a new <see cref="InvalidPartSpecificationException"/>.
        /// </summary>
        /// <param name="aMessage">
        /// A message indicating why the <see cref="InvalidPartSpecificationException"/> ocurred.
        /// </param>
        internal InvalidPartSpecificationException( string aMessage )
            : base( aMessage ) 
        { }

        /// <summary>
        /// Constructs a new <see cref="InvalidPartSpecificationException"/>.
        /// </summary>
        /// <param name="aMessage">
        /// A message indicating why the <see cref="InvalidPartSpecificationException"/> ocurred.
        /// </param>
        /// <param name="aInnerException">
        /// The underlying <see cref="Exception"/> that caused this exception to be thrown.
        /// </param>
        internal InvalidPartSpecificationException( string aMessage, Exception aInnerException )
            : base( aMessage, aInnerException ) 
        { }
    }

    /// <summary>
    /// This <see cref="FabricaException"/> is thrown when the provided value for a
    /// Feature or Property is not compatible (assignable) to the target Feature/Property.
    /// </summary>
    public sealed class TypeMismatchException : FabricaException
    {
        /// <summary>
        /// Constructs a new <see cref="TypeMismatchException"/>.
        /// </summary>
        /// <param name="aTargetName">
        /// The name of the Feature/Property that the Type was incompatible with.
        /// </param>
        /// <param name="aExpectedType">
        /// The expected <see cref="Type"/> for the target Feature/Property.
        /// </param>
        /// <param name="aProvidedType">
        /// The incompatible <see cref="Type"/> whose use was attempted.
        /// </param>
        internal TypeMismatchException( string aTargetName, Type aExpectedType, Type aProvidedType )
            : base( $"For feature/property '{aTargetName}', object of type '{aProvidedType?.Name}' is incompatible with the feature/property type '{aExpectedType?.Name}'." )
        {}

        /// <summary>
        /// Constructs a new <see cref="TypeMismatchException"/>.
        /// </summary>
        /// <param name="aMessage">
        /// A message describing why the <see cref="TypeMismatchException"/> occurred.
        /// </param>
        internal TypeMismatchException( string aMessage )
            : base(aMessage)
        {}
    }

    /// <summary>
    /// This <see cref="FabricaException"/> is thrown when a <see cref="Definition.PartSpecification"/> failed
    /// to be instantiated.
    /// </summary>
    public sealed class FailedPartInstantiationException : FabricaException
    {
        /// <summary>
        /// Construct a new <see cref="FailedPartInstantiationException"/>.
        /// </summary>
        /// <param name="aPartName">
        /// The name of the Part Specification that failed to be instantiated.
        /// </param>
        internal FailedPartInstantiationException( string aPartName )
            : base( $"Failed to instantiate part specification '{aPartName}'." )
        {}

        /// <summary>
        /// Construct a new <see cref="FailedPartInstantiationException"/>.
        /// </summary>
        /// <param name="aPartName">
        /// The name of the Part Specification that failed to be instantiated.
        /// </param>
        /// <param name="aInnerException">
        /// The underlying <see cref="Exception"/> that caused part instantiation to fail.
        /// </param>
        internal FailedPartInstantiationException( string aPartName, Exception aInnerException )
            : base( $"Failed to instantiate part specification '{aPartName}'.", aInnerException )
        {}
    }

    /// <summary>
    /// This <see cref="FabricaException"/> is thrown when a Part could not be assembled
    /// during the <see cref="PartContainer"/> part assembly process. May appear in an
    /// <see cref="AggregateException"/>.
    /// </summary>
    public sealed class FailedPartAssemblyException : FabricaException
    {
        /// <summary>
        /// Constructs a new <see cref="FailedPartAssemblyException"/>.
        /// </summary>
        /// <param name="aPartID">
        /// The ID or Name of the Part that assembly failed for.
        /// </param>
        internal FailedPartAssemblyException( string aPartID )
            : base( $"Failed to assemble part with ID/Name '{aPartID.ToUpper()}'." )
        {}

        /// <summary>
        /// Constructs a new <see cref="FailedPartAssemblyException"/>.
        /// </summary>
        /// <param name="aPartID">
        /// The ID or Name of the Part that assembly failed for.
        /// </param>
        /// <param name="aInnerException">
        /// The underlying <see cref="Exception"/> that caused assembly to fail.
        /// </param>
        internal FailedPartAssemblyException( string aPartID, Exception aInnerException )
            : base( $"Failed to assemble part with ID/Name '{aPartID}'. See inner exception for details.", aInnerException )
        {}


        /// <summary>
        /// Constructs a new <see cref="FailedPartAssemblyException"/>.
        /// </summary>
        /// <param name="aPartID">
        /// The ID or Name of the Part that assembly failed for.
        /// </param>
        /// <param name="aLocationInfo">
        /// A string indicating the location of the part definition in the source
        /// blueprint file.
        /// </param>
        /// <param name="aInnerException">
        /// The underlying <see cref="Exception"/> that caused assembly to fail.
        /// </param>
        internal FailedPartAssemblyException(string aPartID, string aLocationInfo, Exception aInnerException)
            : base($"Failed to assemble part with ID/Name '{aPartID}' at '{aLocationInfo}'. See inner exception for details.", aInnerException)
        { }
    }
}
