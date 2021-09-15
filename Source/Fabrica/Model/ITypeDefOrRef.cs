// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GEAviation.Fabrica.Model 
{
    // TODO > TypeAliases should be phased out in favor of just
    //        simply expecting a TypeDefinition. The file format
    //        should define a alias system, if it wants. The model
    //        shouldn't care.

    // TODO > This interface should be phased out.

    /// <summary>
    /// Used to allow usage of either <see cref="TypeAlias"/> or <see cref="TypeDefinition"/>
    /// without knowing which ahead of time.
    /// </summary>
    public interface ITypeDefOrRef
    {
        // Marker interface. Intentionally empty.
    }
}
