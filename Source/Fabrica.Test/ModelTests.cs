// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using GEAviation.Fabrica.Model;
using NUnit.Framework;

namespace Fabrica.Test
{
    [TestFixture]
    public class ModelTests
    {
        [Test]
        public void testBlueprint()
        {
            Blueprint lBlueprint = new Blueprint();

            lBlueprint.Namespace = "test namespace";

            Assert.IsNotNull( lBlueprint );
            Assert.IsNotNull( lBlueprint.Parts );
            Assert.IsNotNull( lBlueprint.TypeAliases );

            Assert.AreEqual( "test namespace", lBlueprint.Namespace );
        }

        [Test]
        public void testPart()
        {
            Part lPart = new Part();

            Assert.IsNotNull( lPart );

            Assert.IsNotNull( lPart.Features );
            Assert.IsNotNull( lPart.Properties );
            Assert.IsNotNull( lPart.Metadata );
            Assert.AreNotEqual( Guid.Empty, lPart.ID );
            Assert.IsTrue( lPart.HasTemporaryID );

            var lNewGuid = Guid.NewGuid();
            lPart.ID = lNewGuid;
            lPart.LocationScheme = "fake-scheme";
            lPart.Name = "fake.name";

            var lAlias = new TypeAlias()
            {
                AliasName = "alias name"
            };

            lPart.RuntimeType = lAlias;

            var lOtherPart = new Part();
            lOtherPart.Name = "other.test.part";

            lPart.Metadata.Add( "test-metadata-key", "lMetadataValue" );
            lPart.Properties.Add( "test-property-key", new PropertyValue() { Value = "test-property-value" } );

            var lPropUri = new Uri( "test://uri" );

            lPart.Properties.Add( "test-property-key-2", new PropertyValue() { ValueUri = lPropUri } );
            lPart.Features.Add( "test-feature-key", lOtherPart );

            Assert.AreEqual( lNewGuid, lPart.ID );
            Assert.IsFalse( lPart.HasTemporaryID );
            Assert.AreEqual( "fake-scheme", lPart.LocationScheme );
            Assert.AreSame( lAlias, lPart.RuntimeType );
            Assert.AreEqual( "fake.name", lPart.Name );

            lPart.ID = Guid.Empty;
            Assert.IsTrue( lPart.HasTemporaryID );
            Assert.AreNotEqual( Guid.Empty, lPart.ID );

            Assert.AreEqual( "lMetadataValue", lPart.Metadata["test-metadata-key"] );

            Assert.AreEqual( (lPart.Properties["test-property-key"] as PropertyValue)?.Value, "test-property-value" );
            Assert.AreEqual( (lPart.Properties["test-property-key-2"] as PropertyValue)?.ValueUri, lPropUri );

            Assert.AreSame( lPart.Features["test-feature-key"], lOtherPart );
        }

        [Test]
        public void testIDRef()
        {
            IDPartRef lRef = new IDPartRef();

            var lGuid = Guid.NewGuid();

            lRef.PartID = lGuid;

            Assert.AreEqual( lGuid, lRef.PartID );
        }

        [Test]
        public void testNameRef()
        {
            NamedPartRef lRef = new NamedPartRef();
            lRef.PartName = "test-part";
            Assert.AreEqual( "test-part", lRef.PartName );
        }

        [Test]
        public void testUriRef()
        {
            UriPartRef lRef = new UriPartRef();
            Uri lTestUri = new Uri( "fake:/uri/here" );

            lRef.PartUri = lTestUri;

            Assert.AreEqual( lTestUri, lRef.PartUri );
        }

        [Test]
        public void testTypeDefinition()
        {
            TypeDefinition lKeyType = new TypeDefinition();
            lKeyType.FullName = "System.String";

            TypeDefinition lValueType = new TypeDefinition();
            lValueType.FullName = "System.Int32";

            TypeDefinition lOverallType = new TypeDefinition();
            lOverallType.FullName = "System.Collections.Generic.IDictionary`2";
            lOverallType.TypeParameters["TKey"] = lKeyType;
            lOverallType.TypeParameters["TValue"] = lValueType;

            var lType = TypeDefinition.TypeFromDefinition( lOverallType );

            Assert.AreEqual( typeof(IDictionary<string, int>), lType );

            var lTypeDef = TypeDefinition.DefinitionFromType( typeof(IDictionary<int, string>) );

            Assert.AreEqual( lOverallType.FullName, lTypeDef.FullName );
            Assert.AreEqual( lKeyType.FullName, lTypeDef.TypeParameters["TValue"].FullName );
            Assert.AreEqual( lValueType.FullName, lTypeDef.TypeParameters["TKey"].FullName );

            lType = TypeDefinition.TypeFromDefinition( lTypeDef );
            Assert.AreEqual( typeof(IDictionary<int, string>), lType );
        }

        [Test]
        public void testTypeDefWithMissingType()
        {
            // Check what happens if a type can't be found
            TypeDefinition lKeyType = new TypeDefinition();
            lKeyType.FullName = "THIS.ISNT.A.REAL.TYPE`3";

            TypeDefinition lValueType = new TypeDefinition();
            lValueType.FullName = "System.Int32";

            TypeDefinition lOverallType = new TypeDefinition();
            lOverallType.FullName = "System.Collections.Generic.IDictionary`2";
            lOverallType.TypeParameters["TKey"] = lKeyType;
            lOverallType.TypeParameters["TValue"] = lValueType;

            var lType = TypeDefinition.TypeFromDefinition( lOverallType );

            Assert.IsNull( lType );
        }

        [Test]
        public void testTypeDefWithNullArg()
        {
            Assert.Throws( typeof(ArgumentNullException), () => TypeDefinition.TypeFromDefinition( null ) );

            Assert.Throws( typeof(ArgumentNullException), () => TypeDefinition.DefinitionFromType( null ) );
        }

        [Test]
        public void testTypeDefWithGenericDefinition()
        {
            Assert.Throws( typeof(InvalidOperationException), () => TypeDefinition.DefinitionFromType( typeof(IDictionary<,>) ) );
        }

        [Test]
        public void testUndefinedPart()
        {
            UndefinedPart lPart = new UndefinedPart();

            Assert.IsNotNull( lPart );
            Assert.IsNotNull( lPart.Metadata );
            Assert.AreEqual( Guid.Empty, lPart.ID );

            var lNewGuid = Guid.NewGuid();
            lPart.ID = lNewGuid;
            lPart.Name = "fake.name";

            var lOtherPart = new Part();
            lOtherPart.Name = "other.test.part";

            var lMetadataValue = "test-metadata-value";
            lPart.Metadata.Add( "test-metadata-key", lMetadataValue );

            Assert.AreEqual( lNewGuid, lPart.ID );
            Assert.AreEqual( "fake.name", lPart.Name );
            Assert.AreEqual( lMetadataValue, lPart.Metadata["test-metadata-key"] );
        }

        [Test]
        public void testExternalPart()
        {
            ExternalPart lPart = new ExternalPart();

            Assert.IsNotNull( lPart );
            Assert.IsNotNull( lPart.Metadata );
            Assert.AreEqual( Guid.Empty, lPart.ID );

            var lNewGuid = Guid.NewGuid();
            lPart.ID = lNewGuid;
            lPart.Name = "fake.name";
            lPart.LocationScheme = "fake-scheme";

            var lOtherPart = new Part();
            lOtherPart.Name = "other.test.part";

            var lMetadataValue = "test-metadata-value";
            lPart.Metadata.Add( "test-metadata-key", lMetadataValue );

            Assert.AreEqual( lNewGuid, lPart.ID );
            Assert.AreEqual( "fake-scheme", lPart.LocationScheme );
            Assert.AreEqual( "fake.name", lPart.Name );
            Assert.AreEqual(lMetadataValue, lPart.Metadata["test-metadata-key"]);
        }

        [Test]
        public void testTypeAlias()
        {
            TypeAlias lAlias = new TypeAlias();
            lAlias.AliasName = "TYPE_ALIAS";

            TypeDefinition lTypeDef = new TypeDefinition();
            lAlias.Type = lTypeDef;

            Assert.AreEqual( "TYPE_ALIAS", lAlias.AliasName );
            Assert.AreSame( lTypeDef, lAlias.Type );
        }
    }
}
