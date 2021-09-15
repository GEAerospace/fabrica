// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GEAviation.Fabrica.Model 
{
    // TODO > TypeAliases should be phased out in favor of just
    //        simply expecting a TypeDefinition. The file format
    //        should define a alias system, if it wants. The model
    //        shouldn't care.

    /// <summary>
    /// A type alias represents a simple name for a TypeDefinition.
    /// </summary>
    public class TypeAlias : ITypeDefOrRef
    {
        /// <summary>
        /// The simple name for the underlying <see cref="TypeDefinition"/>.
        /// </summary>
        public string AliasName { get; set; }

        /// <summary>
        /// The <see cref="TypeDefinition"/> that this <see cref="TypeAlias"/> represents.
        /// </summary>
        public TypeDefinition Type { get; set; }

        public TypeAlias() { }

        /// <summary>
        /// Copy constructor. Generates a deep copy of the specified <see cref="TypeAlias"/>.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public TypeAlias(TypeAlias aToCopy, bool aShallow = false)
        {
            AliasName = aToCopy.AliasName;
            if(!aShallow && aToCopy.Type != null)
            { 
               Type = new TypeDefinition(aToCopy.Type);
            }
        }
    }
}
