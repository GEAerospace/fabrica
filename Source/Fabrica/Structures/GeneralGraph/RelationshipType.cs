// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GEAviation.Fabrica.Structures.GeneralGraph
{
    /// <summary>
    /// Explains the potential relationship types that two GraphNodes can have, captured and reference by the GraphEdge type.
    /// </summary>
    public class RelationshipType
    {
        public virtual string getRelationshipString()
        {
            return "{0} connects to {1}";
        }
    }
}
