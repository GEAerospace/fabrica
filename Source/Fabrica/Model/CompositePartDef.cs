// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using GEAviation.Fabrica.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GEAviation.Fabrica.Model
{
    public class CompositePartDef : IPartDefOrRef
    {
        public string Name { get; set; }
        public Part RootPart { get; set; }
                
        public CompositePartDef() { }

        /// <summary>
        /// Copy constructor. Generates a deep copy of the specified <see cref="Blueprint"/>.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public CompositePartDef(CompositePartDef aToCopy, bool aShallow = false)
        {
            Name = aToCopy.Name;

            if(!aShallow)
            { 
                RootPart = new Part(aToCopy.RootPart);
            }
        }
    }
}
