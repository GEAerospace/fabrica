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
    public class XmlBlueprintWriter : IBlueprintListFileWriter
    {
        private static readonly XNamespace kFabricaNS;
        private static readonly XmlSchemaSet kBlueprintSchemaSet;

        #if DEBUG
        public static bool OutputAllIDs { get; set; } = false;
        #endif

        public class XRaw : XText
        {
            public XRaw(string text):base(text){}
            public XRaw(XText text): base(text){}

            public override void WriteTo(XmlWriter writer)
            {
                writer.WriteRaw(this.Value);
            }
        }

        static XmlBlueprintWriter()
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

        public static XElement writeBlueprintList( IEnumerable<Blueprint> aBlueprints )
        {
            if (aBlueprints == null)
            {
                throw new ArgumentNullException(nameof(aBlueprints));
            }

            XElement lBlueprintList = new XElement( kFabricaNS + "blueprint-list" );

            foreach( var lBlueprint in aBlueprints )
            {
                var lBlueprintElement = writeBlueprint( lBlueprint );
                if( lBlueprintElement != null )
                {
                    lBlueprintList.Add( lBlueprintElement );
                }
            }

            return lBlueprintList;
        }

        public static XElement writeBlueprint( Blueprint aBlueprint )
        {
            if (aBlueprint == null)
            {
                throw new ArgumentNullException(nameof(aBlueprint));
            }

            XElement lBlueprintElement = new XElement( kFabricaNS + "blueprint" );

            lBlueprintElement.SetAttributeValue( "namespace", aBlueprint.Namespace );

            // Type Aliases
            XElement lTypeAliasesElement = new XElement( kFabricaNS + "type-aliases" );

            foreach( var lAlias in aBlueprint.TypeAliases )
            {
                var lAliasElement = writeTypeAlias( lAlias.Value );
                if( lAliasElement != null )
                {
                    lTypeAliasesElement.Add( lAliasElement );
                }
            }

            lBlueprintElement.Add( lTypeAliasesElement );

            // Composites
            XElement lCompositesElement = new XElement( kFabricaNS + "composites" );

            foreach( var lComposite in aBlueprint.Composites )
            {
                var lCompositeElement = writeCompositeDef( lComposite.Value );
                if( lCompositeElement != null )
                {
                    lCompositesElement.Add( lCompositeElement );
                }
            }

            lBlueprintElement.Add( lCompositesElement );

            // Parts
            XElement lPartsElement = new XElement( kFabricaNS + "parts" );

            foreach (var lPart in aBlueprint.Parts)
            {
                var lPartElement = writeIPart(lPart.Value);
                if (lPartElement != null)
                {
                    lPartsElement.Add(lPartElement);
                }
            }

            lBlueprintElement.Add( lPartsElement );

            return lBlueprintElement;
        }

        private static XElement writeCompositeDef( CompositePartDef aCompositeDef )
        {
            var lCompositeDef = aCompositeDef ?? throw new ArgumentNullException(nameof(aCompositeDef));
            
            XElement lCompositeDefElement = new XElement( kFabricaNS + "composite-def" );
            lCompositeDefElement.SetAttributeValue("name", lCompositeDef.Name);

            var lPart = writePart(lCompositeDef.RootPart);

            if(lPart != null)
            {
                lCompositeDefElement.Add( lPart );
            }

            return lCompositeDefElement;
        }

        public static XElement writeTypeDef( TypeDefinition aTypeDef )
        {
            if (aTypeDef == null)
            {
                throw new ArgumentNullException(nameof(aTypeDef));
            }

            XElement lTypeElement = new XElement( kFabricaNS + "type" );
            lTypeElement.SetAttributeValue( "fullname", aTypeDef.FullName );

            foreach( var lParam in aTypeDef.TypeParameters )
            {
                var lParamElement = writeTypeDef( lParam.Value );
                if( lParamElement != null )
                {
                    lParamElement.Name = kFabricaNS + "type-param";
                    lParamElement.SetAttributeValue( "param-name", lParam.Key );
                    lTypeElement.Add( lParamElement );
                }
            }

            return lTypeElement;
        }

        public static XElement writeTypeAlias( TypeAlias aTypeAlias )
        {
            if (aTypeAlias == null)
            {
                throw new ArgumentNullException(nameof(aTypeAlias));
            }

            XElement lAliasElement = new XElement( kFabricaNS + "alias" );

            lAliasElement.SetAttributeValue( "name", aTypeAlias.AliasName );

            var lTypeDefElement = writeTypeDef( aTypeAlias.Type );

            if(lTypeDefElement != null)
            {
                lAliasElement.Add( lTypeDefElement );
            }

            return lAliasElement;
        }

        public static XElement writeNamedPartRef( NamedPartRef aNamedRef )
        {
            if (aNamedRef == null)
            {
                throw new ArgumentNullException(nameof(aNamedRef));
            }

            XElement lNamedPartRefElement = new XElement( kFabricaNS + "name-ref" );
            lNamedPartRefElement.SetAttributeValue( "name", aNamedRef.PartName );
            return lNamedPartRefElement;
        }

        public static XElement writeUriPartRef( UriPartRef aUriPartRef )
        {
            if (aUriPartRef == null)
            {
                throw new ArgumentNullException(nameof(aUriPartRef));
            }

            XElement lUriPartRefElement = new XElement( kFabricaNS + "uri-ref" );
            lUriPartRefElement.SetAttributeValue( "uri", aUriPartRef.PartUri.ToString() );
            return lUriPartRefElement;
        }

        public static XElement writeIDPartRef( IDPartRef aIDPartRef )
        {
            if (aIDPartRef == null)
            {
                throw new ArgumentNullException(nameof(aIDPartRef));
            }

            XElement lIDPartRefElement = new XElement( kFabricaNS + "id-ref" );
            lIDPartRefElement.SetAttributeValue( "id", aIDPartRef.PartID.ToString().ToUpper() );
            return lIDPartRefElement;
        }

        public static XElement writePartDefOrRef( IPartDefOrRef aDefOrRef, bool aAllowCollections )
        {
            switch( aDefOrRef )
            {
                case NamedPartRef lRef:
                    return writeNamedPartRef( lRef );

                case UriPartRef lRef:
                    return writeUriPartRef( lRef );

                case IDPartRef lRef:
                    return writeIDPartRef( lRef );

                case PartList lPartList when aAllowCollections:
                    return writePartList( lPartList );

                case PartDictionary lPartDictionary when aAllowCollections:
                    return writePartDictionary( lPartDictionary );

                case Part lPart:
                    return writePart( lPart );

                case ConstantValue lConstantValue:
                    return writeConstantValue( lConstantValue );
                
                case FeatureSlot lFeatureSlot:
                    var lFeatureElement = new XElement( kFabricaNS + "feature-slot" );
                    lFeatureElement.SetAttributeValue( "name", lFeatureSlot.SlotName );
                    return lFeatureElement;

                default:
                    return null;
            }
        }

        private static XElement writeConstantValue( ConstantValue aConstantValue )
        {
            XElement lConstantValue = new XElement( kFabricaNS + "constant" );
            lConstantValue.SetAttributeValue( "value", aConstantValue.Value );
            return lConstantValue;
        }

        public static XElement writePartList( PartList aPartList )
        {
            if (aPartList == null)
            {
                throw new ArgumentNullException(nameof(aPartList));
            }

            var lPartListElement = writeGenericPartData( aPartList, true );

            if( lPartListElement != null )
            {
                lPartListElement.Name = kFabricaNS + "part-list";

                XElement lTypeDefOrRef = null;

                if( aPartList.RuntimeType is TypeDefinition lRuntimeTypeDef )
                {
                    lTypeDefOrRef = writeTypeDef( lRuntimeTypeDef );
                    if( lTypeDefOrRef != null )
                    {
                        lTypeDefOrRef.Name = kFabricaNS + "runtime-type";
                    }
                }
                else if( aPartList.RuntimeType is TypeAlias lRuntimeTypeAlias )
                {
                    lTypeDefOrRef = new XElement( kFabricaNS + "runtime-type-alias" );
                    lTypeDefOrRef.SetAttributeValue( "name", lRuntimeTypeAlias.AliasName );
                }

                if( lTypeDefOrRef != null )
                {
                    lPartListElement.Add( lTypeDefOrRef );
                }

                foreach( var lPartDefOrRef in aPartList )
                {
                    var lRefOrDefElement = writePartDefOrRef( lPartDefOrRef, false );

                    if( lRefOrDefElement != null )
                    {
                        lPartListElement.Add( lRefOrDefElement );
                    }
                }

                // Metadata
                var lMetadata = writeMetadata( aPartList.Metadata );
                if( lMetadata != null )
                {
                    lPartListElement.Add( lMetadata );
                }
            }

            return lPartListElement;
        }

        public static XElement writePartDictionary( PartDictionary aPartDictionary )
        {
            if (aPartDictionary == null)
            {
                throw new ArgumentNullException(nameof(aPartDictionary));
            }

            var lPartDictionaryElement = writeGenericPartData( aPartDictionary, true );

            if( lPartDictionaryElement != null )
            {
                lPartDictionaryElement.Name = kFabricaNS + "part-dictionary";

                XElement lTypeDefOrRef = null;

                if( aPartDictionary.RuntimeType is TypeDefinition lRuntimeTypeDef )
                {
                    lTypeDefOrRef = writeTypeDef( lRuntimeTypeDef );
                    if( lTypeDefOrRef != null )
                    {
                        lTypeDefOrRef.Name = kFabricaNS + "runtime-type";
                    }
                }
                else if(aPartDictionary.RuntimeType is TypeAlias lRuntimeTypeAlias )
                {
                    lTypeDefOrRef = new XElement( kFabricaNS + "runtime-type-alias" );
                    lTypeDefOrRef.SetAttributeValue( "name", lRuntimeTypeAlias.AliasName );
                }

                if( lTypeDefOrRef != null )
                {
                    lPartDictionaryElement.Add( lTypeDefOrRef );
                }

                foreach( var lPair in aPartDictionary )
                {
                    var lRefOrDefElement = writePartDefOrRef( lPair.Value, false );

                    if( lRefOrDefElement != null )
                    {
                        XElement lKeyValueElement = new XElement( kFabricaNS + "key-value" );
                        lKeyValueElement.SetAttributeValue( "key", lPair.Key );
                        lKeyValueElement.Add( lRefOrDefElement );

                        lPartDictionaryElement.Add( lKeyValueElement );
                    }
                }

                // Metadata
                var lMetadata = writeMetadata( aPartDictionary.Metadata );
                if( lMetadata != null )
                {
                    lPartDictionaryElement.Add( lMetadata );
                }
            }

            return lPartDictionaryElement;
        }

        //public static XElement writeFeature( IPartDefOrRef aFeatureDefOrRef )
        //{
        //    throw new NotImplementedException();
        //}

        public static XElement writeIPart( IPart aPart )
        {
            if (aPart == null)
            {
                throw new ArgumentNullException(nameof(aPart));
            }

            switch( aPart )
            {
                case ExternalPart lPart:
                    return writeExternalPart( lPart );

                case UndefinedPart lPart:
                    return writeUndefinedPart( lPart );

                case Part lPart:
                    return writePart( lPart );

                case PartList lPart:
                    return writePartList( lPart );

                case PartDictionary lPart:
                    return writePartDictionary( lPart );

                default:
                    return null;
            }
        }

        public static XElement writePart( Part aPart )
        {
            if (aPart == null)
            {
                throw new ArgumentNullException(nameof(aPart));
            }

            var lPartElement = writeGenericPartData( aPart, true );

            if( lPartElement != null )
            {
                lPartElement.Name = kFabricaNS + "part";

                if( !string.IsNullOrWhiteSpace( aPart.LocationScheme ) )
                {
                    lPartElement.SetAttributeValue( "part-locator-scheme", aPart.LocationScheme );
                }

                XElement lTypeDefOrRef = null;

                if( aPart.RuntimeType is TypeDefinition lRuntimeTypeDef )
                {
                    lTypeDefOrRef = writeTypeDef( lRuntimeTypeDef );
                    if( lTypeDefOrRef != null )
                    {
                        lTypeDefOrRef.Name = kFabricaNS + "runtime-type";
                    }
                }
                else if(aPart.RuntimeType is TypeAlias lRuntimeTypeAlias )
                {
                    lTypeDefOrRef = new XElement( kFabricaNS + "runtime-type-alias" );
                    lTypeDefOrRef.SetAttributeValue( "name", lRuntimeTypeAlias.AliasName );
                }
                else if(aPart.RuntimeType is CompositeTypeRef lCompositeRef)
                {
                    lTypeDefOrRef = new XElement( kFabricaNS + "composite-type" );
                    lTypeDefOrRef.SetAttributeValue( "name", lCompositeRef.Name );
                }

                if( lTypeDefOrRef != null )
                {
                    lPartElement.Add( lTypeDefOrRef );
                }

                // Part Constructor
                if( !string.IsNullOrWhiteSpace( aPart.Constructor ) )
                {
                    var lConstructorElement = new XElement( kFabricaNS + "constructor" );
                    lConstructorElement.SetAttributeValue( "name", aPart.Constructor );
                    lPartElement.Add( lConstructorElement );
                }

                // Features
                if( aPart.Features.Count > 0 )
                {
                    var lFeaturesElement = new XElement( kFabricaNS + "features" );

                    foreach( var lFeature in aPart.Features )
                    {
                        if(lFeature.Value is FeatureSlot lFeatureSlot)
                        {
                            var lFeatureElement = new XElement( kFabricaNS + "feature-slot" );
                            lFeatureElement.SetAttributeValue( "target-key", lFeature.Key);
                            lFeatureElement.SetAttributeValue( "name", lFeatureSlot.SlotName );
                            lFeaturesElement.Add( lFeatureElement );
                        }
                        else
                        { 
                            var lFeatureElement = new XElement( kFabricaNS + "feature" );
                            lFeatureElement.SetAttributeValue( "key", lFeature.Key );

                            var lFeatureValue = writePartDefOrRef( lFeature.Value, true );

                            if( lFeatureValue != null )
                            {
                                lFeatureElement.Add( lFeatureValue );
                                lFeaturesElement.Add( lFeatureElement );
                            }
                        }
                    }

                    lPartElement.Add( lFeaturesElement );
                }

                // Properties
                if( aPart.Properties.Count > 0 )
                {
                    var lPropertiesElement = new XElement( kFabricaNS + "properties" );

                    foreach( var lProperty in aPart.Properties )
                    {
                        var lPropertyElement = new XElement( kFabricaNS + "property" );
                        
                        if(lProperty.Value is PropertyValue lPropValue)
                        { 
                            lPropertyElement.SetAttributeValue( "key", lProperty.Key );
                            if( lPropValue.ValueUri != null )
                            {
                                lPropertyElement.SetAttributeValue( "uri", lPropValue.ValueUri.ToString() );
                            }
                            else if( lPropValue.Value != null )
                            {
                                lPropertyElement.SetAttributeValue( "value", lPropValue.Value );
                            }
                        }
                        else if(lProperty.Value is PropertySlot lPropSlot)
                        {
                            lPropertyElement.Name = kFabricaNS + "property-slot";
                            lPropertyElement.SetAttributeValue( "target-key", lProperty.Key );
                            lPropertyElement.SetAttributeValue( "name", lPropSlot.SlotName );
                        }
                        lPropertiesElement.Add( lPropertyElement );
                    }

                    lPartElement.Add( lPropertiesElement );
                }

                // Metadata
                var lMetadata = writeMetadata( aPart.Metadata );
                if( lMetadata != null )
                {
                    lPartElement.Add( lMetadata );
                }
            }

            return lPartElement;
        }

        public static XElement writeGenericPartData( IPart aPart, bool aSkipMetadata = false )
        {
            if (aPart == null)
            {
                throw new ArgumentNullException(nameof(aPart));
            }

            XElement lPartElement = new XElement( kFabricaNS + "invalid-part" );

            if( !string.IsNullOrWhiteSpace(aPart.Name) )
            {
                lPartElement.SetAttributeValue( "name", aPart.Name );
            }

            #if !DEBUG
            if( aPart.ID != Guid.Empty && (!(aPart is ICanHaveTemporaryID lPartWithTempID) || !lPartWithTempID.HasTemporaryID) )
            #else
            if( aPart.ID != Guid.Empty && ((!(aPart is ICanHaveTemporaryID lPartWithTempID) || !lPartWithTempID.HasTemporaryID) || OutputAllIDs ) )
            #endif
            {
                lPartElement.SetAttributeValue( "id", aPart.ID.ToString().ToUpper() );
            }

            if( !aSkipMetadata )
            {
                var lMetadata = writeMetadata( aPart.Metadata );

                if( lMetadata != null )
                {
                    lPartElement.Add( lMetadata );
                }
            }

            return lPartElement;
        }

        public static XElement writeUndefinedPart( UndefinedPart aUndefinedPart )
        {
            if (aUndefinedPart == null)
            {
                throw new ArgumentNullException(nameof(aUndefinedPart));
            }

            var lUndefinedPartElement = writeGenericPartData( aUndefinedPart );

            if( lUndefinedPartElement != null )
            {
                lUndefinedPartElement.Name = kFabricaNS + "undefined-part";
            }

            return lUndefinedPartElement;
        }

        public static XElement writeExternalPart( ExternalPart aExternalPart )
        {
            if (aExternalPart == null)
            {
                throw new ArgumentNullException(nameof(aExternalPart));
            }

            var lExternalPartElement = writeGenericPartData( aExternalPart );
            if( lExternalPartElement != null )
            {
                lExternalPartElement.Name = kFabricaNS + "external-part";
                if( !string.IsNullOrWhiteSpace( aExternalPart.LocationScheme ) )
                {
                    lExternalPartElement.SetAttributeValue( "part-locator-scheme", aExternalPart.LocationScheme );
                }
            }

            return lExternalPartElement;
        }

        public static XElement writeMetadata( IDictionary<string, string> aMetadata )
        {
            if (aMetadata == null)
            {
                throw new ArgumentNullException(nameof(aMetadata));
            }

            if( aMetadata.Count == 0 )
            {
                // No metadata!
                return null;
            }

            XElement lMetadataElement = new XElement( kFabricaNS + "metadata" );

            foreach( var lPair in aMetadata )
            {
                XElement lDataElement = new XElement( kFabricaNS + "data" );
                lDataElement.SetAttributeValue( "key", lPair.Key );

                XCData lCData = new XCData( lPair.Value );

                lDataElement.Add( lCData );

                lMetadataElement.Add( lDataElement );
            }

            return lMetadataElement;
        }

        public bool writeBlueprintsToFile( string aPath, IEnumerable<Blueprint> aBlueprints, IList<BlueprintIOError> aParseErrors )
        {
            var lXml = writeBlueprintList( aBlueprints );
            if( lXml != null )
            {
                using( FileStream lFS = new FileStream( aPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None ) )
                {
                    lXml.Save( lFS );
                }
            }

            // The only thing that indicates failure here is an exception
            // for which, this line won't be reached.
            return true;
        }
    }
}
