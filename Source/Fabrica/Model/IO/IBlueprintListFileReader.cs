// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GEAviation.Fabrica.Model.IO
{
    public interface IBlueprintListFileReader
    {
        IEnumerable<Blueprint> readBlueprintsFromFile( string aPath, IList<BlueprintIOError> aReadErrors );
    }
}
