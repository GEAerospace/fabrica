// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace GEAviation.Fabrica.Model 
{
    /// <summary>
    /// This class represents a Part Reference to a Part
    /// with a specific ID.
    /// </summary>
    public class IDPartRef : IPartDefOrRef
    {
        /// <summary>
        /// The ID of the Part that this Part Reference points to.
        /// </summary>
        public Guid PartID { get; set; }
                
        public IDPartRef() { }

        public IDPartRef(IDPartRef aToCopy)
        {
            PartID = aToCopy.PartID;
        }
    }
}
