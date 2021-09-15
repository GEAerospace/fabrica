// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GEAviation.Fabrica;
using GEAviation.Fabrica.Definition;
using GEAviation.Fabrica.Model.IO;
using NUnit.Framework;

namespace Fabrica.Test
{
    [TestFixture]
    public class PartContainerTests
    {
        [Test]
        public void loadNormal()
        {
            var lGoodParts = CompileTestParts.getGoodPartsAssembly();

            var lXmlPath = Path.Combine( TestContext.CurrentContext.TestDirectory, "simple-set.xml" );

            var lOneExtern = new ExternalPartInstance( new List<int> { 1, 2 }, Guid.Parse( "93FB0623-744D-4EED-B782-2D5E084D2F8B" ) );

            System.Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;

            var lContainer = FabricaFacade.loadPartContainer<XmlBlueprintReader>(lXmlPath,
                                                                                  new List<Assembly> { lGoodParts },
                                                                                  new List<ExternalPartInstance> { lOneExtern }, out var lAssemblyExceptions );
            Assert.IsNull( lAssemblyExceptions );

            Assert.AreEqual(12, lContainer.PartsByName.Count);

            // Check what was loaded in the container.
        }
    }
}
