// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace GEAviation.Fabrica.Model 
{
    /// <summary>
    /// This class represents a Part Reference to a Part
    /// with Uri. A Part Locator for the Uri's Scheme must exist in 
    /// order to resolve the Part.
    /// </summary>
    public class UriPartRef : IPartDefOrRef
    {
        /// <summary>
        /// The Uri of the Part that this Part Reference points to.
        /// </summary>
        public Uri PartUri { get; set; }

        public UriPartRef() { }

        /// <summary>
        /// Copy constructor. Generates a deep copy of the specified <see cref="UriPartRef"/>.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public UriPartRef(UriPartRef aToCopy)
        {
            PartUri = aToCopy.PartUri;
        }
    }
}
