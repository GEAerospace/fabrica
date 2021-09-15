// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using GEAviation.Fabrica.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GEAviation.Fabrica.Model
{
    public class CompositeTypeRef : ITypeDefOrRef
    {
        public string Name { get; set; }
        
        public CompositeTypeRef() { }

        /// <summary>
        /// Copy constructor. Generates a deep copy of the specified <see cref="Blueprint"/>.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public CompositeTypeRef(CompositeTypeRef aToCopy)
        {
            Name = aToCopy.Name;
        }
    }
}
