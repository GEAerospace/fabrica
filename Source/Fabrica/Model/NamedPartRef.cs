// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GEAviation.Fabrica.Model 
{
    /// <summary>
    /// This class represents a Part Reference to a Part
    /// with a specific Name.
    /// </summary>
    public class NamedPartRef : IPartDefOrRef
    {
        /// <summary>
        /// The name of the Part that this Part Reference points to.
        /// </summary>
        public string PartName { get; set; }
        
        public NamedPartRef() { }

        /// <summary>
        /// Copy constructor. Generates a deep copy of the specified <see cref="NamedPartRef"/>.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public NamedPartRef(NamedPartRef aToCopy)
        {
            PartName = aToCopy.PartName;
        }
    }
}
