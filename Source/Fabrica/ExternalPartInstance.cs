// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using GEAviation.Fabrica.Definition;

namespace GEAviation.Fabrica 
{
    /// <summary>
    /// This class represents a single External Part Instance to be registered with a <see cref="PartContainer"/>
    /// when calling any overload of assembleParts in <see cref="PartContainer"/>.
    /// </summary>
    public struct ExternalPartInstance
    {
        /// <summary>
        /// Gets the Name (if available) of this External Part. 
        /// Value will be <see cref="string.Empty"/> if not specified.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the ID of this External Part.
        /// Value will be generated (via <see cref="Guid.NewGuid"/>) if not specified.
        /// </summary>
        public Guid ID { get; }

        /// <summary>
        /// Get the Uri Scheme (if available) that this External Part handles,
        /// provided it's a part locator.
        /// Value will be <see cref="string.Empty"/> if not specified.
        /// </summary>
        public string LocationScheme { get; }

        /// <summary>
        /// Gets the External Part Instance that this <see cref="ExternalPartInstance"/> represents.
        /// </summary>
        public object PartInstance { get; }

        /// <summary>
        /// Used internally to provide "contractural" alternatives to the full constructor.
        /// Helps ensure a valid <see cref="ExternalPartInstance"/> gets created.
        /// </summary>
        private ExternalPartInstance( object aPartInstance, Guid aID, string aName, string aLocationScheme )
        {
            if( aID == Guid.Empty )
            {
                throw new ArgumentException( "Cannot be the Empty Guid.", nameof(aID) );
            }

            PartInstance = aPartInstance ?? throw new ArgumentNullException(nameof(aPartInstance));
            ID = aID;
            Name = aName ?? throw new ArgumentNullException(nameof(aName));
            LocationScheme = aLocationScheme ?? throw new ArgumentNullException(nameof(aLocationScheme));
        }

        /// <summary>
        /// Used internally to provide "contractural" alternatives to the full constructor.
        /// Helps ensure a valid <see cref="ExternalPartInstance"/> gets created.
        /// </summary>
        private ExternalPartInstance( object aPartInstance, Guid aID, string aLocationScheme )
            : this( aPartInstance, aID, string.Empty, aLocationScheme )
        { }

        /// <summary>
        /// Used internally to provide "contractural" alternatives to the full constructor.
        /// Helps ensure a valid <see cref="ExternalPartInstance"/> gets created.
        /// </summary>
        private ExternalPartInstance( object aPartInstance, string aName, string aLocationScheme )
            : this( aPartInstance, Guid.NewGuid(), aName, aLocationScheme )
        { }

        /// <summary>
        /// Creates a <see cref="ExternalPartInstance"/> with a ID number and 
        /// Location Uri Scheme. This constructor is for registering "external" Part Locators
        /// when assembling parts in <see cref="PartContainer"/>.
        /// </summary>
        /// <param name="aPartInstance">
        /// The <see cref="IPartLocator"/> to register.
        /// </param>
        /// <param name="aID">
        /// The ID number to give to this instance.
        /// </param>
        /// <param name="aLocationScheme">
        /// The Uri Scheme that the <see cref="IPartLocator"/> can locate parts for.
        /// </param>
        public ExternalPartInstance( IPartLocator aPartInstance, Guid aID, string aLocationScheme )
            : this(aPartInstance, aID, String.Empty, aLocationScheme)
        { }

        /// <summary>
        /// Creates a <see cref="ExternalPartInstance"/> with a Name and 
        /// Location Uri Scheme. This constructor is for registering "external" Part Locators
        /// when assembling parts in <see cref="PartContainer"/>.
        /// </summary>
        /// <param name="aPartInstance">
        /// The <see cref="IPartLocator"/> to register.
        /// </param>
        /// <param name="aName">
        /// The Name to give to this instance.
        /// </param>
        /// <param name="aLocationScheme">
        /// The Uri Scheme that the <see cref="IPartLocator"/> can locate parts for.
        /// </param>
        public ExternalPartInstance( IPartLocator aPartInstance, string aName, string aLocationScheme )
            : this(aPartInstance, Guid.NewGuid(), aName, aLocationScheme)
        { }
        
        /// <summary>
        /// Creates a <see cref="ExternalPartInstance"/> with an ID.
        /// </summary>
        /// <param name="aPartInstance">
        /// The <see cref="IPartLocator"/> to register.
        /// </param>
        /// <param name="aID">
        /// The ID number to give to this instance.
        /// </param>
        public ExternalPartInstance( object aPartInstance, Guid aID )
            : this(aPartInstance, aID, string.Empty)
        { }

        /// <summary>
        /// Creates a <see cref="ExternalPartInstance"/> with an ID.
        /// </summary>
        /// <param name="aPartInstance">
        /// The <see cref="IPartLocator"/> to register.
        /// </param>
        /// <param name="aName">
        /// The Name to give to this instance.
        /// </param>
        public ExternalPartInstance( object aPartInstance, string aName )
            : this(aPartInstance, aName, string.Empty)
        { }
    }
}