// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using GEAviation.Fabrica.Model.IO;
using NUnit.Framework;

using System.Linq;

namespace Fabrica.Test.IO
{
    [TestFixture]
    public class XmlBlueprintReaderTests
    {
        [Test]
        public void fullFeatureBlueprint()
        {
            XmlBlueprintReader lReader = new XmlBlueprintReader();
            List<BlueprintIOError> lErrors = new List<BlueprintIOError>();

            var lInputPath = Path.Combine( TestContext.CurrentContext.TestDirectory, "IO\\fully-featured-blueprints.xml" );

            var lModel = lReader.readBlueprintsFromFile( lInputPath, lErrors );

            // Verify the model
        }

        [Test]
        public void argNulls()
        {

        }
    }
}
