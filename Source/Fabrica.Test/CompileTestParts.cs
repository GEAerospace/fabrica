// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GEAviation.Fabrica.Definition;

namespace Fabrica.Test
{
    public static class CompileTestParts
    {
        public static Assembly getPartsAssembly(string aResourceName)
        {
            // Creating this dummy def to force load the main Fabrica dll...
            //var lDummy = PartSpecification.createPartSpecification( typeof(SpecTestPart1) );

            // Snagged from: https://stackoverflow.com/questions/24871955/c-sharp-compilerresults-generateinmemory
            var lReferencedAssemblies =
                AppDomain.CurrentDomain.GetAssemblies()
                         .Where(a => !a.FullName.StartsWith("mscorlib", StringComparison.InvariantCultureIgnoreCase))
                         .Where(a => !a.IsDynamic) //necessary because a dynamic assembly will throw and exception when calling a.Location
                         .Select(a => a.Location)
                         .ToArray();

            CompilerParameters lCompileParams = new CompilerParameters(lReferencedAssemblies)
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };

            var lCodeStream = Assembly.GetExecutingAssembly().GetManifestResourceStream( aResourceName );
            string lGoodPartsCode = string.Empty;

            using( StreamReader lSR = new StreamReader( lCodeStream ) )
            {
                lGoodPartsCode = lSR.ReadToEnd();
            }

            CompilerResults lCompilationResults = CodeDomProvider.CreateProvider("CSharp")
                                                                 .CompileAssemblyFromSource(lCompileParams, lGoodPartsCode);

            return lCompilationResults.CompiledAssembly;
        }

        public static Assembly getGoodPartsAssembly()
        {
            return getPartsAssembly( "Fabrica.Test.GoodTestParts.Code.cs" );
        }
    }
}
