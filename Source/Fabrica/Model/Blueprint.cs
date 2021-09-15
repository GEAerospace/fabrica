// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GEAviation.Fabrica.Model
{
    /// <summary>
    /// Represents a Blueprint in the Fabrica Data Model. 
    /// Blueprints contain Parts.
    /// </summary>
    public class Blueprint
    {
        /// <summary>
        /// The namespace of the blueprint. Must be unique among
        /// other blueprints in the same model.
        /// </summary>
        public string Namespace { get; set; }

        // TODO > TypeAliases should be phased out in favor of just
        //        simply expecting a TypeDefinition. The file format
        //        should define a alias system, if it wants. The model
        //        shouldn't care.

        /// <summary>
        /// Type alias objects that can be used to make declaring
        /// types throughout a blueprint configuration easier.
        /// </summary>
        public IDictionary<string, TypeAlias> TypeAliases { get; }

        /// <summary>
        /// Type alias objects that can be used to make declaring
        /// types throughout a blueprint configuration easier.
        /// </summary>
        public IDictionary<string, CompositePartDef> Composites { get; }

        /// <summary>
        /// Parts are the individual components of a Blueprint. They
        /// represent desired objects that should be constructed via
        /// the Fabrica system. Parts can also be "injected" into other
        /// Parts.
        /// </summary>
        /// <remarks>
        /// This collection only represents "root" Parts that are defined
        /// at the highest level. These Parts may define additional Parts
        /// as part of their features. As a result these represent sub-graphs
        /// of the overall Blueprint.
        /// </remarks>
        public IDictionary<Guid, IPart> Parts { get; }

        /// <summary>
        /// Contructs a new Blueprint object.
        /// </summary>
        public Blueprint()
        {
            TypeAliases = new Dictionary<string, TypeAlias>();
            Composites = new Dictionary<string, CompositePartDef>();
            Parts = new Dictionary<Guid, IPart>();
        }

        /// <summary>
        /// Copy constructor. Generates a deep copy of the specified <see cref="Blueprint"/>.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public Blueprint(Blueprint aToCopy, bool aShallow = false)
            : this()
        {
            Namespace = aToCopy.Namespace;

            if(!aShallow)
            { 
                foreach(var lAlias in aToCopy.TypeAliases)
                {
                    TypeAliases[lAlias.Key] = new TypeAlias(lAlias.Value);
                }

                foreach(var lComposite in aToCopy.Composites)
                {
                    Composites[lComposite.Key] = new CompositePartDef(lComposite.Value);
                }

                foreach(var lPart in aToCopy.Parts)
                {
                    Parts[lPart.Key] = lPart.Value.createCopy();
                }
            }
        }
    }
}
