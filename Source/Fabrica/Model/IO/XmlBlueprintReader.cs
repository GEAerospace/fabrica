// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace GEAviation.Fabrica.Model.IO
{
    /// <summary>
    /// This class can read and validate an XML file containing Blueprint data and
    /// build a Blueprint model from that data. This can also be used to read
    /// individual parts of a blueprint XML file. This can be used to read
    /// metadata in a part that is specified in XML format.
    /// </summary>
    public class XmlBlueprintReader : IBlueprintListFileReader
    {
        private static readonly XNamespace kFabricaNS;
        private static readonly XmlSchemaSet kBlueprintSchemaSet;

        static XmlBlueprintReader()
        {
            var lSchemaSet = new XmlSchemaSet();
            var lSchemaPath = "GEAviation.Fabrica.blueprint.xsd";

            using( var lSchemaStream = typeof(XmlBlueprintReader).Assembly.GetManifestResourceStream( lSchemaPath ) )
            {
                if( lSchemaStream == null )
                {
                    throw new InvalidOperationException( "Could not get embedded stream for blueprint XML schema." );
                }

                var lSchema = lSchemaSet.Add( null, XmlReader.Create( lSchemaStream ) );

                if( lSchema == null )
                {
                    throw new InvalidOperationException( "Failed to load blueprint XML schema." );
                }

                kFabricaNS = lSchema.TargetNamespace;
                kBlueprintSchemaSet = lSchemaSet;
            }
        }

        /// <summary>
        /// Returns the Line/Column information for an XML object,
        /// provided such data is available.
        /// </summary>
        /// <param name="aXMLObject">
        /// The XML object to retrieve Line/Column data for.
        /// </param>
        /// <returns>
        /// "Line #, Column #" if there is line info for the XML object,
        /// "Line ?, Column ?" otherwise.
        /// </returns>
        private static string getElementLocation( IXmlLineInfo aXMLObject )
        {
            if( aXMLObject?.HasLineInfo() ?? false )
            {
                return $"Line {aXMLObject.LineNumber}, Column {aXMLObject.LinePosition}";
            }

            return "Line ?, Column ?";
        }

        /// <summary>
        /// Reads a Blueprint List from an XML file specified by the file path.
        /// </summary>
        /// <param name="aFilePath">
        /// The path to the file. 
        /// </param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns>
        /// A list of <see cref="Blueprint"/> objects representing the contents of the
        /// XML file.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the path is null, empty, or the file doesn't exist.
        /// </exception>
        public IEnumerable<Blueprint> readBlueprintsFromFile( string aFilePath, IList<BlueprintIOError> aParseErrors )
        {
            if( string.IsNullOrWhiteSpace( aFilePath ) )
            {
                throw new ArgumentException( "File path cannot be null or empty.", nameof(aFilePath) );
            }

            if( !File.Exists( aFilePath ) )
            {
                throw new ArgumentException( $"File '{aFilePath}' could not be found.", nameof(aFilePath) );
            }

            using( StreamReader lSR = new StreamReader( aFilePath ) )
            {
                var lDocument = XDocument.Load( lSR, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace );
                return readBlueprintsFromXml( lDocument, aParseErrors );
            }
        }

        /// <summary>
        /// Reads a Blueprint List from an <see cref="XDocument"/>.
        /// </summary>
        /// <param name="aDocument">
        /// The <see cref="XDocument"/> containing the blueprint list.
        /// </param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns>
        /// A list of <see cref="Blueprint"/> objects representing the contents of the
        /// <see cref="XDocument"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified <see cref="XDocument"/> is null.
        /// </exception>
        public IEnumerable<Blueprint> readBlueprintsFromXml( XDocument aDocument, IList<BlueprintIOError> aParseErrors )
        {
            if( aDocument == null )
            {
                throw new ArgumentNullException( nameof(aDocument) );
            }

            bool lValid = true;

            aDocument.Validate( kBlueprintSchemaSet,
                                ( aObject, aArgs ) =>
                                {
                                    var lSeverity = aArgs.Severity == XmlSeverityType.Warning
                                                        ? BlueprintProblemSeverity.Warning
                                                        : BlueprintProblemSeverity.Error;

                                    aParseErrors?.Add( new BlueprintIOError( lSeverity, $"XML Validation: '{aArgs.Message}'." ) );
                                    lValid = false;
                                } );

            if( !lValid )
            {
                throw new InvalidOperationException( "Validation failed for blueprint definition XML." );
            }

            return readBlueprintList( aDocument.Root, aParseErrors );
        }

        /// <summary>
        /// Reads a blueprint-list element, and it's children, from an XML Element.
        /// </summary>
        /// <param name="aBlueprintListElement">
        /// A <see cref="XElement"/> representing the blueprint-list.
        /// </param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns>
        /// A collection of <see cref="Blueprint"/> elements representing the set
        /// of blueprint elements in the provided blueprint-list.
        /// </returns>
        public static IEnumerable<Blueprint> readBlueprintList( XElement aBlueprintListElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aBlueprintListElement == null )
            {
                throw new ArgumentNullException( nameof(aBlueprintListElement) );
            }

            var lBlueprintList = new List<Blueprint>();

            var lBlueprints = aBlueprintListElement.Elements().Where( aElement => aElement.Name.LocalName == "blueprint" );

            foreach( var lBlueprintElement in lBlueprints )
            {
                var lBlueprint = readBlueprint( lBlueprintElement, aParseErrors );

                if( lBlueprint != null )
                {
                    lBlueprintList.Add( lBlueprint );
                }
                else
                {
                    aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"Failed to read blueprint at {getElementLocation( lBlueprintElement )}" ) );
                }
            }

            GlobalProperties.FileLocationInfo.setValue( lBlueprintList, getElementLocation( aBlueprintListElement ) );
            return lBlueprintList;
        }

        /// <summary>
        /// Reads a type-def object from an XML element.
        /// </summary>
        /// <param name="aElement">
        /// The element representing the type-def.
        /// </param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns>
        /// A <see cref="TypeDefinition"/> object that the original XML represented.
        /// </returns>
        public static TypeDefinition readTypeDef( XElement aElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aElement == null )
            {
                throw new ArgumentNullException( nameof(aElement) );
            }

            var lTypeName = aElement.Attribute( "fullname" )?.Value;

            if( !string.IsNullOrWhiteSpace( lTypeName ) )
            {
                TypeDefinition lTypeDef = new TypeDefinition();
                lTypeDef.FullName = lTypeName;

                foreach( var lParamDefElement in aElement.Elements() )
                {
                    var lParamName = lParamDefElement.Attribute( "param-name" )?.Value;
                    var lParamTypeDef = readTypeDef( lParamDefElement, aParseErrors );

                    if( !( string.IsNullOrWhiteSpace( lParamName ) || lParamTypeDef == null ) )
                    {
                        lTypeDef.TypeParameters[lParamName] = lParamTypeDef;
                    }
                }

                GlobalProperties.FileLocationInfo.setValue( lTypeDef, getElementLocation( aElement ) );
                return lTypeDef;
            }

            aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"Failed to decode Type reference '{aElement}' at {getElementLocation( aElement )}." ) );
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aElement"></param>
        /// <param name="aParseErrors"></param>
        /// <returns></returns>
        public static CompositePartDef readCompositeDef( XElement aElement, IList<BlueprintIOError> aParseErrors )
        {
            var lCompositeName = aElement.Attribute( "name" )?.Value;
            var lElementLocation = getElementLocation( aElement );
                
            if(lCompositeName != null)
            {
                CompositePartDef lDef = new CompositePartDef();
                lDef.Name = lCompositeName;
                
                // there should only be one root part, but just in case...
                var lRootParts = aElement.Elements().Where( aChild => aChild.Name.LocalName == "part" );
                var lNumRootParts = lRootParts.Count();

                if(lNumRootParts == 1)
                {
                    var lRootPart = lRootParts.First();

                    lDef.RootPart = readPart( lRootPart, aParseErrors );

                    GlobalProperties.FileLocationInfo.setValue( lDef, lElementLocation );
                    return lDef;
                }
                else if(lNumRootParts > 1)
                {
                    aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"'{aElement}' at {lElementLocation} has too many parts. Composite definitions can only have one root part." ) );
                }
                else
                { 
                    aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"'{aElement}' at {lElementLocation} has no root part." ) );
                }
            }
            else
            {
                aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"'{aElement}' at {lElementLocation} doesn't have a name." ) );
            }

            return null;
        }

        /// <summary>
        /// Reads a type alias from an XML element.
        /// </summary>
        /// <param name="aElement">
        /// The element representing the type alias.
        /// </param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns>
        /// A <see cref="TypeAlias"/> that the XML element represented.
        /// </returns>
        public static TypeAlias readTypeAlias( XElement aElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aElement == null )
            {
                throw new ArgumentNullException( nameof(aElement) );
            }

            var lAliasName = aElement.Attribute( "name" )?.Value;
            var lAliasTypeDefElement = aElement.Element( kFabricaNS + "type" );

            TypeDefinition lTypeDef = null;

            if( lAliasTypeDefElement != null )
            {
                lTypeDef = readTypeDef( lAliasTypeDefElement, aParseErrors );
            }

            TypeAlias lAlias = new TypeAlias();
            lAlias.AliasName = lAliasName;
            lAlias.Type = lTypeDef;

            GlobalProperties.FileLocationInfo.setValue( lAlias, getElementLocation( aElement ) );
            return lAlias;
        }

        /// <summary>
        /// Reads a single blueprint definition from an XML element.
        /// </summary>
        /// <param name="aBlueprintElement">
        /// The element that represents the blueprint.
        /// </param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns>
        /// A <see cref="Blueprint"/> object based on the provided XML element.
        /// </returns>
        public static Blueprint readBlueprint( XElement aBlueprintElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aBlueprintElement == null )
            {
                throw new ArgumentNullException( nameof(aBlueprintElement) );
            }

            var lBlueprint = new Blueprint();

            lBlueprint.Namespace = aBlueprintElement.Attribute( "namespace" )?.Value;

            var lPartsList = aBlueprintElement.Element( kFabricaNS + "parts" );

            if( lPartsList != null )
            {
                foreach( var lPartDefinitionElement in lPartsList.Elements() )
                {
                    IPart lPart = null;

                    switch( lPartDefinitionElement.Name.LocalName )
                    {
                        case "undefined-part":
                            lPart = readUndefinedPart( lPartDefinitionElement, aParseErrors );
                            break;

                        case "external-part":
                            lPart = readExternalPart( lPartDefinitionElement, aParseErrors );
                            break;

                        case "part":
                            lPart = readPart( lPartDefinitionElement, aParseErrors );
                            break;

                        case "part-list":
                            lPart = readPartList( lPartDefinitionElement, aParseErrors );
                            break;

                        case "part-dictionary":
                            lPart = readPartDictionary( lPartDefinitionElement, aParseErrors );
                            break;

                        default:
                            aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"Unsupported part type '{lPartDefinitionElement.Name.LocalName}'." ) );
                            return null;
                    }

                    if( lPart != null )
                    {
                        var lFinalPartID = lPart.ID;

                        if( lPart.ID == Guid.Empty )
                        {
                            lFinalPartID = Guid.NewGuid();
                        }

                        if( lBlueprint.Parts.ContainsKey( lFinalPartID ) )
                        {
                            aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Warning, $"Part with ID '{lFinalPartID}' is a duplicated. Only one will be loaded." ) );
                        }

                        lBlueprint.Parts[lFinalPartID] = lPart;
                    }
                }
            }

            var lTypeAliasList = aBlueprintElement.Element( kFabricaNS + "type-aliases" );

            if( lTypeAliasList != null )
            {
                foreach( var lAliasElement in lTypeAliasList.Elements() )
                {
                    var lTypeAlias = readTypeAlias( lAliasElement, aParseErrors );

                    if( lTypeAlias != null )
                    {
                        lBlueprint.TypeAliases[lTypeAlias.AliasName] = lTypeAlias;
                    }
                }
            }

            var lCompositeDefList = aBlueprintElement.Element( kFabricaNS + "composites" );

            if( lCompositeDefList != null )
            {
                foreach( var lCompositeDefElement in lCompositeDefList.Elements() )
                {
                    var lCompositeDef = readCompositeDef( lCompositeDefElement, aParseErrors );

                    if( lCompositeDef != null )
                    {
                        lBlueprint.Composites[lCompositeDef.Name] = lCompositeDef;
                    }
                }
            }

            GlobalProperties.FileLocationInfo.setValue( lBlueprint, getElementLocation( aBlueprintElement ) );
            return lBlueprint;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aNamedRefElement"></param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns></returns>
        public static NamedPartRef readNamedPartRef( XElement aNamedRefElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aNamedRefElement == null )
            {
                throw new ArgumentNullException( nameof(aNamedRefElement) );
            }

            var lName = aNamedRefElement.Attribute( "name" )?.Value;

            if( !string.IsNullOrWhiteSpace( lName ) )
            {
                NamedPartRef lRef = new NamedPartRef();
                lRef.PartName = lName;
                GlobalProperties.FileLocationInfo.setValue( lRef, getElementLocation( aNamedRefElement ) );
                return lRef;
            }

            aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"Failed to decode Name reference '{aNamedRefElement}' at {getElementLocation( aNamedRefElement )}." ) );
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aUriRefElement"></param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns></returns>
        public static UriPartRef readUriPartRef( XElement aUriRefElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aUriRefElement == null )
            {
                throw new ArgumentNullException( nameof(aUriRefElement) );
            }

            var lURI = aUriRefElement.Attribute( "uri" )?.Value;

            if( !string.IsNullOrWhiteSpace( lURI ) )
            {
                try
                {
                    UriPartRef lRef = new UriPartRef();
                    lRef.PartUri = new Uri( lURI, UriKind.RelativeOrAbsolute );
                    GlobalProperties.FileLocationInfo.setValue( lRef, getElementLocation( aUriRefElement ) );
                    return lRef;
                }
                catch( Exception lException )
                {
                    aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"{lException.Message}" ) );
                }
            }

            aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"Failed to decode URI reference '{aUriRefElement}' at {getElementLocation( aUriRefElement )}." ) );
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aIDRefElement"></param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns></returns>
        public static IDPartRef readIDPartRef( XElement aIDRefElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aIDRefElement == null )
            {
                throw new ArgumentNullException( nameof(aIDRefElement) );
            }

            var lGuid = aIDRefElement.Attribute( "id" )?.Value;

            if( !string.IsNullOrWhiteSpace( lGuid ) )
            {
                try
                {
                    IDPartRef lRef = new IDPartRef();
                    lRef.PartID = Guid.Parse( lGuid );
                    GlobalProperties.FileLocationInfo.setValue( lRef, getElementLocation( aIDRefElement ) );
                    return lRef;
                }
                catch( Exception lException )
                {
                    aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"{lException.Message}" ) );
                }
            }

            aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"Failed to decode ID reference '{aIDRefElement}' at {getElementLocation( aIDRefElement )}." ) );
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aElement"></param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns></returns>
        private static IPartDefOrRef readPartCollectionElement( XElement aElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aElement == null )
            {
                throw new ArgumentNullException( nameof(aElement) );
            }

            IPartDefOrRef lPart = null;
            switch( aElement.Name.LocalName )
            {
                case "name-ref":
                    lPart = readNamedPartRef( aElement, aParseErrors );
                    break;

                case "id-ref":
                    lPart = readIDPartRef( aElement, aParseErrors );
                    break;

                case "uri-ref":
                    lPart = readUriPartRef( aElement, aParseErrors );
                    break;

                case "part":
                    lPart = readPart( aElement, aParseErrors );
                    break;

                case "constant":
                    lPart = readConstantValue( aElement, aParseErrors );
                    break;

                case "feature-slot":
                    { 
                        var lSlotName = aElement.Attribute( "name" )?.Value;

                        if(lSlotName != null)
                        { 
                            lPart = new FeatureSlot() {  SlotName = lSlotName };
                        }
                    }
                    break;

                default:
                    aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"Unrecognized part in part '{aElement.Name.LocalName}' in part list." ) );
                    break;
            }

            return lPart;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aElement"></param>
        /// <param name="aParseErrors"></param>
        /// <returns></returns>
        public static IPartDefOrRef readConstantValue( XElement aElement, IList<BlueprintIOError> aParseErrors )
        {
            var lValueString = aElement.Attribute( "value" )?.Value;
            var lConstantValue = new ConstantValue() { Value = lValueString };
            GlobalProperties.FileLocationInfo.setValue( lConstantValue, getElementLocation( aElement ) );
            return lConstantValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aPartListElement"></param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns></returns>
        public static PartList readPartList( XElement aPartListElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aPartListElement == null )
            {
                throw new ArgumentNullException( nameof(aPartListElement) );
            }

            var lIDString   = aPartListElement.Attribute( "id" )?.Value;
            var lNameString = aPartListElement.Attribute( "name" )?.Value;

            PartList lList = new PartList();

            foreach( var lChildElement in aPartListElement.Elements() )
            {
                switch( lChildElement.Name.LocalName )
                {
                    case "runtime-type":
                        lList.RuntimeType = readTypeDef( lChildElement, aParseErrors );
                        break;

                    case "runtime-type-alias":
                        lList.RuntimeType = readTypeAlias( lChildElement, aParseErrors );
                        break;

                    case "metadata":
                        readMetadata(lList, lChildElement, aParseErrors);

                        break;

                    default:
                        IPartDefOrRef lPart = readPartCollectionElement( lChildElement, aParseErrors );

                        if( lPart != null )
                        {
                            lList.Add( lPart );
                        }
                        break;
                }
            }

            if( lIDString != null )
            {
                lList.ID = Guid.Parse( lIDString );
            }

            if( lNameString != null )
            {
                lList.Name = lNameString;
            }

            GlobalProperties.FileLocationInfo.setValue( lList, getElementLocation( aPartListElement ) );
            return lList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aPartDictionaryElement"></param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns></returns>
        public static PartDictionary readPartDictionary( XElement aPartDictionaryElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aPartDictionaryElement == null )
            {
                throw new ArgumentNullException( nameof(aPartDictionaryElement) );
            }

            var lIDString   = aPartDictionaryElement.Attribute( "id" )?.Value;
            var lNameString = aPartDictionaryElement.Attribute( "name" )?.Value;

            PartDictionary lDictionary = new PartDictionary();

            foreach( var lChildElement in aPartDictionaryElement.Elements() )
            {
                switch( lChildElement.Name.LocalName )
                {   
                    case "runtime-type":
                        lDictionary.RuntimeType = readTypeDef( lChildElement, aParseErrors );
                        break;

                    case "runtime-type-alias":
                        lDictionary.RuntimeType = readTypeAlias( lChildElement, aParseErrors );
                        break;

                    case "metadata":
                        readMetadata(lDictionary, lChildElement, aParseErrors);

                        break;

                    default:
                        var lKey = lChildElement.Attribute( "key" )?.Value;

                        if( lKey != null && lChildElement.Elements().FirstOrDefault() is var lValueElement )
                        {
                            IPartDefOrRef lPart = readPartCollectionElement( lValueElement, aParseErrors );

                            if( lPart != null )
                            {
                                lDictionary[lKey] = lPart;
                            }
                        }

                        break;
                }
            }
            
            if( lIDString != null )
            {
                lDictionary.ID = Guid.Parse( lIDString );
            }

            if( lNameString != null )
            {
                lDictionary.Name = lNameString;
            }

            GlobalProperties.FileLocationInfo.setValue( lDictionary, getElementLocation( aPartDictionaryElement ) );
            return lDictionary;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aFeatureElement"></param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns></returns>
        public static IPartDefOrRef readFeature( XElement aFeatureElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aFeatureElement == null )
            {
                throw new ArgumentNullException( nameof(aFeatureElement) );
            }

            switch( aFeatureElement.Name.LocalName )
            {
                case "part-list":
                    return readPartList( aFeatureElement, aParseErrors );

                case "part-dictionary":
                    return readPartDictionary( aFeatureElement, aParseErrors );

                default:
                    return readPartCollectionElement( aFeatureElement, aParseErrors );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aPartElement"></param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns></returns>
        public static Part readPart( XElement aPartElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aPartElement == null )
            {
                throw new ArgumentNullException( nameof(aPartElement) );
            }

            var lIDString = aPartElement.Attribute( "id" )?.Value;
            var lNameString = aPartElement.Attribute( "name" )?.Value;
            var lLocatorScheme = aPartElement.Attribute( "part-locator-scheme" )?.Value;

            Part lPart = new Part();

            foreach( var lChildElement in aPartElement.Elements() )
            {
                switch( lChildElement.Name.LocalName )
                {
                    case "runtime-type":
                        lPart.RuntimeType = readTypeDef( lChildElement, aParseErrors );
                        break;

                    case "runtime-type-alias":
                        lPart.RuntimeType = readTypeAlias( lChildElement, aParseErrors );
                        break;

                    case "composite-type":
                        lPart.RuntimeType = readCompositeTypeRef( lChildElement, aParseErrors );
                        break;

                    case "constructor":
                        lPart.Constructor = lChildElement.Attribute( "name" )?.Value ?? String.Empty;
                        break;

                    case "features":
                        foreach( var lFeatureElement in lChildElement.Elements() )
                        {
                            var lKey = lFeatureElement.Attribute( "key" )?.Value;

                            IPartDefOrRef lFeature = null;

                            if( lFeatureElement.Name.LocalName == "feature-slot" )
                            {
                                lKey = lFeatureElement.Attribute( "target-key" )?.Value;
                                var lSlotName = lFeatureElement.Attribute( "name" )?.Value;

                                if(lSlotName != null)
                                { 
                                    lFeature = new FeatureSlot() {  SlotName = lSlotName };
                                }
                            }
                            else if( lFeatureElement.Elements().FirstOrDefault() is var lFeatureValue )
                            {
                                lFeature = readFeature( lFeatureValue, aParseErrors );    
                            }

                            if( lKey != null && lFeature != null )
                            {
                                lPart.Features[lKey] = lFeature;
                            }
                        }

                        break;

                    case "metadata":
                        readMetadata( lPart, lChildElement, aParseErrors );

                        break;

                    case "properties":
                        foreach( var lPropertyElement in lChildElement.Elements() )
                        {
                            string lKey = null;
                            IPropertyValueOrSlot lPropValue = null;

                            if(lPropertyElement.Name.LocalName == "property-slot")
                            {
                                lKey = lPropertyElement.Attribute( "target-key" )?.Value;
                                var lSlotName = lPropertyElement.Attribute( "name" )?.Value;

                                if(lSlotName != null)
                                {
                                    lPropValue = new PropertySlot() { SlotName = lSlotName };
                                }
                            }
                            else
                            { 
                                lKey = lPropertyElement.Attribute( "key" )?.Value;
                                var lValue = lPropertyElement.Attribute( "value" )?.Value;
                                var lUri = lPropertyElement.Attribute( "uri" )?.Value;

                                if( lValue != null ^ lUri != null )
                                {
                                    Uri lFinalUri = null;

                                    if( lUri != null )
                                    {
                                        lFinalUri = new Uri( lUri );
                                    }

                                    lPropValue = new PropertyValue()
                                    {
                                        Value = lValue,
                                        ValueUri = lFinalUri
                                    };
                                }
                            }

                            if( lKey != null && lPropValue != null )
                            {
                                lPart.Properties[lKey] = lPropValue;
                            }
                        }

                        break;
                }
            }

            if( lIDString != null )
            {
                lPart.ID = Guid.Parse( lIDString );
            }

            if( lNameString != null )
            {
                lPart.Name = lNameString;
            }

            if( lLocatorScheme != null )
            {
                lPart.LocationScheme = lLocatorScheme;
            }

            GlobalProperties.FileLocationInfo.setValue( lPart, getElementLocation( aPartElement ) );
            return lPart;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aElement"></param>
        /// <param name="aParseErrors"></param>
        /// <returns></returns>
        private static CompositeTypeRef readCompositeTypeRef( XElement aElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aElement == null )
            {
                throw new ArgumentNullException( nameof(aElement) );
            }

            var lCompositeTypeName = aElement.Attribute( "name" )?.Value;

            CompositeTypeRef lTypeRef = new CompositeTypeRef();
            lTypeRef.Name = lCompositeTypeName;

            GlobalProperties.FileLocationInfo.setValue( lTypeRef, getElementLocation( aElement ) );
            return lTypeRef;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aPart"></param>
        /// <param name="aChildElement"></param>
        /// <param name="aParseErrors"></param>
        public static void readMetadata( IPart aPart, XElement aChildElement, IList<BlueprintIOError> aParseErrors )
        {
            foreach (var lMetadataElement in aChildElement.Elements())
            {
                var lKey = lMetadataElement.Attribute("key")?.Value;
                var lValue = default(string);

                // for "safety" we'll required that all metadata values be in a CDATA section. Otherwise,
                // the content is ignored and the metadata key/value is "purged" on read.
                foreach (var lNode in lMetadataElement.Nodes())
                {
                    if (lNode is XCData lCData)
                    {
                        lValue = lCData.Value;
                    }
                }

                if (lKey != null && lValue != null)
                {
                    aPart.Metadata[lKey] = lValue;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aPartElement"></param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns></returns>
        public static UndefinedPart readUndefinedPart( XElement aPartElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aPartElement == null )
            {
                throw new ArgumentNullException( nameof(aPartElement) );
            }

            var lIDString = aPartElement.Attribute( "id" )?.Value;
            var lNameString = aPartElement.Attribute( "name" )?.Value;

            UndefinedPart lPart = new UndefinedPart();

            foreach( var lChildElement in aPartElement.Elements() )
            {
                switch( lChildElement.Name.LocalName )
                {
                    case "metadata":
                        readMetadata( lPart, lChildElement, aParseErrors );

                        break;
                }
            }

            if( lIDString != null )
            {
                lPart.ID = Guid.Parse( lIDString );
            }

            if( lNameString != null )
            {
                lPart.Name = lNameString;
            }

            GlobalProperties.FileLocationInfo.setValue( lPart, getElementLocation( aPartElement ) );
            return lPart;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aPartElement"></param>
        /// <param name="aParseErrors">
        /// If provided, this argument will be filled with any warnings/errors that occur
        /// while parsing.
        /// </param>
        /// <returns></returns>
        public static ExternalPart readExternalPart( XElement aPartElement, IList<BlueprintIOError> aParseErrors )
        {
            if( aPartElement == null )
            {
                throw new ArgumentNullException( nameof(aPartElement) );
            }

            var lIDString = aPartElement.Attribute( "id" )?.Value;
            var lNameString = aPartElement.Attribute( "name" )?.Value;
            var lLocatorScheme = aPartElement.Attribute( "part-locator-scheme" )?.Value;

            ExternalPart lPart = new ExternalPart();

            foreach( var lChildElement in aPartElement.Elements() )
            {
                switch( lChildElement.Name.LocalName )
                {
                    case "metadata":
                        readMetadata( lPart, lChildElement, aParseErrors );

                        break;
                }
            }

            int lIDOrNameCount = 0;

            if( lIDString != null )
            {
                lPart.ID = Guid.Parse( lIDString );
                lIDOrNameCount++;
            }

            if( lNameString != null )
            {
                lPart.Name = lNameString;
                lIDOrNameCount++;
            }

            if( lIDOrNameCount == 2 )
            {
                aParseErrors?.Add( new BlueprintIOError( BlueprintProblemSeverity.Error, $"External part declaration cannot contain both a name and an ID, at {getElementLocation( aPartElement )}. " ) );
                return null;
            }

            if( lLocatorScheme != null )
            {
                lPart.LocationScheme = lLocatorScheme;
            }

            GlobalProperties.FileLocationInfo.setValue( lPart, getElementLocation( aPartElement ) );
            return lPart;
        }
    }
}
