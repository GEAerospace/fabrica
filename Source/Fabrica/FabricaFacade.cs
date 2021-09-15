// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GEAviation.Fabrica.Definition;
using GEAviation.Fabrica.Model.IO;

namespace GEAviation.Fabrica
{
    /// <summary>
    /// This class implements facade functionality for the Fabrica system.
    /// While most parts of Fabrica can be used individually, this class
    /// brings the pieces together to make average use of Fabrica simple.
    /// </summary>
    public static class FabricaFacade
    {
        /// <summary>
        /// This method encompasses all of the steps necessary to successfully load a 
        /// <see cref="PartContainer"/>. While these steps can be performed individually
        /// by the caller, this method simplifies the process into one common call.
        /// </summary>
        /// <typeparam name="ReaderType">
        /// The reader type to use for reading the supplied file. This must be
        /// a <see cref="IBlueprintListFileReader"/> with a public parameterless contructor.
        /// </typeparam>
        /// <param name="aFilePath">
        /// The path to the file to load the part container from.
        /// </param>
        /// <param name="aPartSpecifications">
        /// A collection of <see cref="IPartSpecification"/> objects that represent the parts that
        /// can be loaded into the <see cref="PartContainer"/>.
        /// </param>
        /// <param name="aExternalParts">
        /// A collection of <see cref="ExternalPartInstance"/> objects representing
        /// parts that are manually loaded by the caller for use within the <see cref="PartContainer"/>.
        /// </param>
        /// <param name="aNonFatalExceptions">
        /// This out parameter will contain an <see cref="AggregateException"/> object generated from the
        /// part assembly process if it failed to assembly all fully-defined parts. If no exceptions occurred,
        /// this parameter will be null.
        /// </param>        
        /// <param name="aLogTimes">
        /// When true, will cause Fabrica to generate a log of how long each part took to assemble/instantiate.
        /// </param>
        /// <param name="aTimeLogPath">
        /// When <paramref name="aLogTimes"/> is true, this path identifies the file the time log will be written to.
        /// </param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown if the <paramref name="aFilePath"/> is null, empty or whitespace.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown if <paramref name="aPartSpecifications"/> is null.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// This exception is thrown if the file pointed to by <paramref name="aFilePath"/> doesn't exist.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if no <see cref="IPartSpecification"/> objects existed within 
        /// the <paramref name="aPartSpecifications"/> collection.
        /// </exception>
        /// <exception cref="AggregateException">
        /// This exception can be thrown for multiple reasons, but the primary cause within this method
        /// is if the <see cref="IBlueprintListFileReader"/> generates any <see cref="BlueprintIOError"/> objects
        /// during the read process.
        /// </exception>
        /// <returns>
        /// A pre-assembled <see cref="PartContainer"/>. If any non-fatal exceptions occurred during assembly,
        /// the part container may be partially filled and the <paramref name="aNonFatalExceptions"/> parameter
        /// will contain an exception that has more information.
        /// </returns>
        public static PartContainer loadPartContainer<ReaderType>( string aFilePath, IEnumerable<IPartSpecification> aPartSpecifications, IEnumerable<ExternalPartInstance> aExternalParts, out AggregateException aNonFatalExceptions, bool aLogTimes = false, string aTimeLogPath = "" )
            where ReaderType : IBlueprintListFileReader, new()
        {
            // Lots of argument checking...
            if ( string.IsNullOrWhiteSpace( aFilePath ) )
            {
                throw new ArgumentException( "String cannot be null, empty or whitespace.", nameof( aFilePath ) );
            }

            if ( !File.Exists( aFilePath ) )
            {
                throw new FileNotFoundException( $"The provided blueprint file '{aFilePath}' doesn't exist." );
            }

            if ( aPartSpecifications == null )
            {
                throw new ArgumentNullException( nameof( aPartSpecifications ) );
            }

            // Load list of external parts. It's ok if this list was null.
            var lExternalParts = aExternalParts;

            if ( lExternalParts == null )
            {
                lExternalParts = new List<ExternalPartInstance>();
            }

            // Attempt to get Part Specs from the provided assemblies.
            var lPartSpecs = aPartSpecifications.ToList();

            if ( lPartSpecs.Count == 0 )
            {
                throw new InvalidOperationException( "Could not find any parts in the source assemblies." );
            }

            // Attempt to read the provided blueprints file.
            var lReader = new ReaderType();
            var lReaderErrors = new List<BlueprintIOError>();

            var lBlueprints = lReader.readBlueprintsFromFile( aFilePath, lReaderErrors );

            // Throw an exception for any non-Warnings that were generated during blueprint reading.
            var lActualReaderErrors = lReaderErrors.Where( aError => aError.Severity != BlueprintProblemSeverity.Warning ).ToList();

            if ( lActualReaderErrors.Count > 0 )
            {
                var lReaderExceptions = new List<Exception>();
                foreach ( var lError in lActualReaderErrors )
                {
                    lReaderExceptions.Add( new Exception( lError.Message ) );
                }

                throw new AggregateException( "One or more errors occurred while reading the blueprints file.", lReaderExceptions );
            }

            aNonFatalExceptions = null;

            // Attempt to generate the PartContainer
            PartContainer lContainer = new PartContainer( lPartSpecs, lBlueprints );

            if( aLogTimes )
            {
                lContainer.LogPartInstantiationTimes = true;
                lContainer.PartInstantiationTimesLogPath = !string.IsNullOrWhiteSpace( aTimeLogPath ) ? aTimeLogPath : lContainer.PartInstantiationTimesLogPath;
            }

            try
            {
                lContainer.assembleParts( lExternalParts );
            }
            catch ( AggregateException lException )
            {
                aNonFatalExceptions = lException;
            }

            return lContainer;
        }

        /// <summary>
        /// This method encompasses all of the steps necessary to successfully load a 
        /// <see cref="PartContainer"/>. While these steps can be performed individually
        /// by the caller, this method simplifies the process into one common call.
        /// </summary>
        /// <typeparam name="ReaderType">
        /// The reader type to use for reading the supplied file. This must be
        /// a <see cref="IBlueprintListFileReader"/> with a public parameterless contructor.
        /// </typeparam>
        /// <param name="aFilePath">
        /// The path to the file to load the part container from.
        /// </param>
        /// <param name="aPartSpecifications">
        /// A collection of <see cref="IPartSpecification"/> objects that represent the specifications for parts
        /// that could be loaded into the <see cref="PartContainer"/>.
        /// </param>
        /// <param name="aExternalParts">
        /// A collection of <see cref="ExternalPartInstance"/> objects representing
        /// parts that are manually loaded by the caller for use within the <see cref="PartContainer"/>.
        /// </param>
        /// <param name="aNonFatalExceptions">
        /// This out parameter will contain an <see cref="AggregateException"/> object generated from the
        /// part assembly process if it failed to assembly all fully-defined parts. If no exceptions occurred,
        /// this parameter will be null.
        /// </param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown if the <paramref name="aFilePath"/> is null, empty or whitespace;
        /// OR, if <paramref name="aPartSources"/> contains no <see cref="Assembly"/> objects.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown if <paramref name="aPartSources"/> is null.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// This exception is thrown if the file pointed to by <paramref name="aFilePath"/> doesn't exist.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if no <see cref="PartSpecification"/> objects could be generated from
        /// the types in the <see cref="Assembly"/> objects provided by <paramref name="aPartSources"/>.
        /// This indicates that no valid Parts exist in any of the provided assemblies.
        /// </exception>
        /// <exception cref="AggregateException">
        /// This exception can be thrown for multiple reasons, but the primary cause within this method
        /// is if the <see cref="IBlueprintListFileReader"/> generates any <see cref="BlueprintIOError"/> objects
        /// during the read process.
        /// </exception>
        /// <returns>
        /// A pre-assembled <see cref="PartContainer"/>. If any non-fatal exceptions occurred during assembly,
        /// the part container may be partially filled and the <paramref name="aNonFatalExceptions"/> parameter
        /// will contain an exception that has more information.
        /// </returns>
        public static PartContainer loadPartContainer<ReaderType>( string aFilePath, IEnumerable<IPartSpecification> aPartSpecifications, IEnumerable<ExternalPartInstance> aExternalParts, out AggregateException aNonFatalExceptions )
            where ReaderType : IBlueprintListFileReader, new()
        {
            return loadPartContainer<ReaderType>( aFilePath, aPartSpecifications, aExternalParts, out aNonFatalExceptions, false, String.Empty );
        }

        /// <summary>
        /// This method encompasses all of the steps necessary to successfully load a 
        /// <see cref="PartContainer"/>. While these steps can be performed individually
        /// by the caller, this method simplifies the process into one common call.
        /// </summary>
        /// <typeparam name="ReaderType">
        /// The reader type to use for reading the supplied file. This must be
        /// a <see cref="IBlueprintListFileReader"/> with a public parameterless contructor.
        /// </typeparam>
        /// <param name="aFilePath">
        /// The path to the file to load the part container from.
        /// </param>
        /// <param name="aPartSources">
        /// A collection of <see cref="Assembly"/> objects that represent the assemblies
        /// containing parts that could be loaded into the <see cref="PartContainer"/>.
        /// </param>
        /// <param name="aExternalParts">
        /// A collection of <see cref="ExternalPartInstance"/> objects representing
        /// parts that are manually loaded by the caller for use within the <see cref="PartContainer"/>.
        /// </param>
        /// <param name="aNonFatalExceptions">
        /// This out parameter will contain an <see cref="AggregateException"/> object generated from the
        /// part assembly process if it failed to assembly all fully-defined parts. If no exceptions occurred,
        /// this parameter will be null.
        /// </param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown if the <paramref name="aFilePath"/> is null, empty or whitespace;
        /// OR, if <paramref name="aPartSources"/> contains no <see cref="Assembly"/> objects.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown if <paramref name="aPartSources"/> is null.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// This exception is thrown if the file pointed to by <paramref name="aFilePath"/> doesn't exist.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if no <see cref="PartSpecification"/> objects could be generated from
        /// the types in the <see cref="Assembly"/> objects provided by <paramref name="aPartSources"/>.
        /// This indicates that no valid Parts exist in any of the provided assemblies.
        /// </exception>
        /// <exception cref="AggregateException">
        /// This exception can be thrown for multiple reasons, but the primary cause within this method
        /// is if the <see cref="IBlueprintListFileReader"/> generates any <see cref="BlueprintIOError"/> objects
        /// during the read process.
        /// </exception>
        /// <returns>
        /// A pre-assembled <see cref="PartContainer"/>. If any non-fatal exceptions occurred during assembly,
        /// the part container may be partially filled and the <paramref name="aNonFatalExceptions"/> parameter
        /// will contain an exception that has more information.
        /// </returns>
        public static PartContainer loadPartContainer<ReaderType>( string aFilePath, IEnumerable<Assembly> aPartSources, IEnumerable<ExternalPartInstance> aExternalParts, out AggregateException aNonFatalExceptions )
            where ReaderType : IBlueprintListFileReader, new()
        {
            return loadPartContainer<ReaderType>( aFilePath, aPartSources, aExternalParts, out aNonFatalExceptions, false, String.Empty );
        }

        /// <summary>
        /// This method encompasses all of the steps necessary to successfully load a 
        /// <see cref="PartContainer"/>. While these steps can be performed individually
        /// by the caller, this method simplifies the process into one common call.
        /// </summary>
        /// <typeparam name="ReaderType">
        /// The reader type to use for reading the supplied file. This must be
        /// a <see cref="IBlueprintListFileReader"/> with a public parameterless contructor.
        /// </typeparam>
        /// <param name="aFilePath">
        /// The path to the file to load the part container from.
        /// </param>
        /// <param name="aPartSources">
        /// A collection of <see cref="Assembly"/> objects that represent the assemblies
        /// containing parts that could be loaded into the <see cref="PartContainer"/>.
        /// </param>
        /// <param name="aExternalParts">
        /// A collection of <see cref="ExternalPartInstance"/> objects representing
        /// parts that are manually loaded by the caller for use within the <see cref="PartContainer"/>.
        /// </param>
        /// <param name="aNonFatalExceptions">
        /// This out parameter will contain an <see cref="AggregateException"/> object generated from the
        /// part assembly process if it failed to assembly all fully-defined parts. If no exceptions occurred,
        /// this parameter will be null.
        /// </param>
        /// <param name="aLogTimes">
        /// When true, will cause Fabrica to generate a log of how long each part took to assemble/instantiate.
        /// </param>
        /// <param name="aTimeLogPath">
        /// When <paramref name="aLogTimes"/> is true, this path identifies the file the time log will be written to.
        /// </param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown if the <paramref name="aFilePath"/> is null, empty or whitespace;
        /// OR, if <paramref name="aPartSources"/> contains no <see cref="Assembly"/> objects.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown if <paramref name="aPartSources"/> is null.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// This exception is thrown if the file pointed to by <paramref name="aFilePath"/> doesn't exist.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if no <see cref="PartSpecification"/> objects could be generated from
        /// the types in the <see cref="Assembly"/> objects provided by <paramref name="aPartSources"/>.
        /// This indicates that no valid Parts exist in any of the provided assemblies.
        /// </exception>
        /// <exception cref="AggregateException">
        /// This exception can be thrown for multiple reasons, but the primary cause within this method
        /// is if the <see cref="IBlueprintListFileReader"/> generates any <see cref="BlueprintIOError"/> objects
        /// during the read process.
        /// </exception>
        /// <returns>
        /// A pre-assembled <see cref="PartContainer"/>. If any non-fatal exceptions occurred during assembly,
        /// the part container may be partially filled and the <paramref name="aNonFatalExceptions"/> parameter
        /// will contain an exception that has more information.
        /// </returns>
        public static PartContainer loadPartContainer<ReaderType>( string aFilePath, IEnumerable<Assembly> aPartSources, IEnumerable<ExternalPartInstance> aExternalParts, out AggregateException aNonFatalExceptions, bool aLogTimes, string aTimeLogPath )
            where ReaderType : IBlueprintListFileReader, new()
        {
            if ( aPartSources == null )
            {
                throw new ArgumentNullException( nameof( aPartSources ) );
            }

            // Load list of source assemblies.
            var lPartSourceList = aPartSources.ToList();

            if ( lPartSourceList.Count == 0 )
            {
                throw new ArgumentException( "Must provide at least one part source assembly in the collection.", nameof( aPartSources ) );
            }

            // Attempt to get Part Specs from the provided assemblies.
            var lPartSpecs = PartSpecification.getSpecifications( lPartSourceList, out var lPartSpecExceptions ).ToList();

            if ( lPartSpecs.Count == 0 )
            {
                throw new InvalidOperationException( "Could not find any parts in the source assemblies." );
            }

            var lContainer = loadPartContainer<ReaderType>( aFilePath, lPartSpecs, aExternalParts, out var lAssemblyExceptions, aLogTimes, aTimeLogPath );

            List<Exception> lAllExceptions = new List<Exception>();

            if ( lPartSpecExceptions != null )
            {
                lAllExceptions.AddRange( lPartSpecExceptions.InnerExceptions );
            }

            if ( lAssemblyExceptions != null )
            {
                lAllExceptions.AddRange( lAssemblyExceptions.InnerExceptions );
            }

            if ( lAllExceptions.Count > 0 )
            {
                aNonFatalExceptions = new AggregateException( "One or more exceptions occurred while loading the part container.", lAllExceptions );
            }
            else
            {
                aNonFatalExceptions = null;
            }

            return lContainer;
        }
    }
}
