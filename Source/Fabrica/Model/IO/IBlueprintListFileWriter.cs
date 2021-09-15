// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace GEAviation.Fabrica.Model.IO 
{
    public interface IBlueprintListFileWriter
    {
        bool writeBlueprintsToFile( string aPath, IEnumerable<Blueprint> aBlueprints, IList<BlueprintIOError> aWriteErrors );
    }
}