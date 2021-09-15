// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GEAviation.Fabrica;
using GEAviation.Fabrica.Definition;
using GEAviation.Fabrica.Model;
using NUnit.Framework;

namespace Fabrica.Test
{
    [Part]
    [GEAviation.Fabrica.Definition.Description("This is a test part for example purposes.")]
    public class SpecTestPart1 : IPropertiesSetNotification
    {
        public bool ExecutedNamedConstructor {get; private set;}= false;

        public string WrappedObject { get; }
        public int Feature2 { get; }

        [GEAviation.Fabrica.Definition.Property]
        public double TestProp { get; set; } = 0.0;

        [GEAviation.Fabrica.Definition.Property( Required = true )]
        [GEAviation.Fabrica.Definition.Description("TestProp2 Description.")]
        public TimeSpan TestProp2 { get; set; } = TimeSpan.Zero;

        [PartConstructor]
        public SpecTestPart1( [Feature("Object")] 
                              [GEAviation.Fabrica.Definition.Description("Object Description.")]
                              string aObject, 
                              
                              [Feature("OptionalObject", Required = false)]
                              [GEAviation.Fabrica.Definition.Description("OptionalObject Description.")]
                              int aFeature2 )
        {
            WrappedObject = aObject;
            Feature2 = aFeature2;
        }

        [PartConstructor( "TestNamedConstructor" )]
        public SpecTestPart1( [Feature( "DifferentObject" )] [GEAviation.Fabrica.Definition.Description( "Different Object Description." )]
                              string aObject )
        {
            WrappedObject = aObject;
            ExecutedNamedConstructor = true;
        }

        public void propertiesSet()
        {
            // Intentionally empty.
        }
    }

    [Part]
    public class SpecTestPart1_PropertiesSetThrows : IPropertiesSetNotification
    {
        [PartConstructor]
        public SpecTestPart1_PropertiesSetThrows()
        {}

        public void propertiesSet()
        {
            throw new NotImplementedException();
        }
    }

    [Part]
    public class SpecTestPart_ReadOnlyProperty
    {
        [GEAviation.Fabrica.Definition.Property]
        public double TestProp { get; } = 0.0;

        [PartConstructor]
        public SpecTestPart_ReadOnlyProperty() { }
    }

    [Part]
    public class SpecTestPart_UnmarkedFeature
    {
        [GEAviation.Fabrica.Definition.Property]
        public double TestProp { get; set; } = 0.0;


        [PartConstructor]
        public SpecTestPart_UnmarkedFeature( object aUnmarkedFeatureValue ) { }
    }

    [Part]
    public class SpecTestPart_NoPartConstructor
    {
        [GEAviation.Fabrica.Definition.Property]
        public double TestProp { get; set; } = 0.0;

        public SpecTestPart_NoPartConstructor( object aUnmarkedFeatureValue ) { }
    }

    public class SpecTestPart_NotAPart
    {
        [GEAviation.Fabrica.Definition.Property]
        public double TestProp { get; set; } = 0.0;

        [PartConstructor]
        public SpecTestPart_NotAPart( [Feature("Object")] object aUnmarkedFeatureValue ) { }
    }

    [Part]
    public class SpecTestPart1_Generic<DataType>
    {
        [PartConstructor]
        public SpecTestPart1_Generic( )
        {}
    }

    [Part]
    public class SpecTestPart1_SetterThrows
    {
        [GEAviation.Fabrica.Definition.Property]
        public double TestProp 
        {
            get => 0.0;
            set => throw new NotImplementedException();
        }

        [PartConstructor]
        public SpecTestPart1_SetterThrows()
        {}
    }

    [Part]
    public class SpecTestPart1_ConstructorThrows
    {
        [PartConstructor]
        public SpecTestPart1_ConstructorThrows()
        {
            throw new NotImplementedException();
        }
    }

    [Part]
    public class SpecTestPart1_MultipleDefaultConstructors
    {
        [PartConstructor]
        public SpecTestPart1_MultipleDefaultConstructors()
        {
            throw new NotImplementedException();
        }

        [PartConstructor]
        public SpecTestPart1_MultipleDefaultConstructors( [Feature( "Special" )] object aSpecial )
        {
            throw new NotImplementedException();
        }

        [PartConstructor("NamedConstructor")]
        public SpecTestPart1_MultipleDefaultConstructors( [Feature( "Special" )] string aSpecial )
        {
            throw new NotImplementedException();
        }
    }

    [Part]
    public class SpecTestPart1_DuplicateNamedConstructors
    {
        [PartConstructor]
        public SpecTestPart1_DuplicateNamedConstructors()
        {
            throw new NotImplementedException();
        }

        [PartConstructor("NamedConstructor")]
        public SpecTestPart1_DuplicateNamedConstructors( [Feature( "Special" )] object aSpecial )
        {
            throw new NotImplementedException();
        }

        [PartConstructor("NamedConstructor")]
        public SpecTestPart1_DuplicateNamedConstructors( [Feature( "Special" )] string aSpecial )
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    [TestFixture]
    public class PartSpecificationTests
    {
        [Test]
        public void testPartTypeAndName()
        {
            IPartSpecification lSpec = PartSpecification.createPartSpecification( typeof(SpecTestPart1) );

            Assert.AreEqual( typeof(SpecTestPart1).FullName, lSpec.Name );
            Assert.AreEqual( typeof(SpecTestPart1), lSpec.PartType );

            Assert.AreEqual( "This is a test part for example purposes.", lSpec.PartDescription );
        }

        [Test]
        public void testPartFeatureAnalysis()
        {
            IPartSpecification lSpec = PartSpecification.createPartSpecification( typeof(SpecTestPart1) );
 
            // Check Feature Stuff
            Assert.AreEqual( 2, lSpec.DefaultConstructor.Features.Count );
            Assert.AreEqual( typeof(string), lSpec.DefaultConstructor.Features["Object"] );
            Assert.AreEqual( typeof(int), lSpec.DefaultConstructor.Features["OptionalObject"] );

            Assert.AreEqual( 2, lSpec.DefaultConstructor.FeatureDescriptions.Count );
            Assert.AreEqual( "Object Description.", lSpec.DefaultConstructor.FeatureDescriptions["Object"] );
            Assert.AreEqual( "OptionalObject Description.", lSpec.DefaultConstructor.FeatureDescriptions["OptionalObject"] );
            
            Assert.AreEqual( 1, lSpec.DefaultConstructor.RequiredFeatures.Count );
            Assert.AreEqual( typeof(string), lSpec.DefaultConstructor.RequiredFeatures["Object"] );

            Assert.AreEqual( 1, lSpec.NamedPartConstructors.Count );

            IPartConstructorInfo lNamedInfo = lSpec.NamedPartConstructors["TestNamedConstructor"];

            Assert.AreEqual( 1, lNamedInfo.Features.Count );
            Assert.AreEqual( typeof(string), lNamedInfo.Features["DifferentObject"] );

            Assert.AreEqual( 1, lNamedInfo.FeatureDescriptions.Count );
            Assert.AreEqual( "Different Object Description.", lNamedInfo.FeatureDescriptions["DifferentObject"] );
            
            Assert.AreEqual( 1, lNamedInfo.RequiredFeatures.Count );
            Assert.AreEqual( typeof(string), lNamedInfo.RequiredFeatures["DifferentObject"] );
        }

        [Test]
        public void testPartPropertyAnalysis()
        {
            IPartSpecification lSpec = PartSpecification.createPartSpecification( typeof(SpecTestPart1) );
            
            // Check Property Stuff
            Assert.AreEqual( 2, lSpec.Properties.Count );
            Assert.AreEqual( typeof(double), lSpec.Properties["TestProp"] );
            Assert.AreEqual( typeof(TimeSpan), lSpec.Properties["TestProp2"] );

            Assert.AreEqual( 1, lSpec.PropertyDescriptions.Count );
            Assert.AreEqual( "TestProp2 Description.", lSpec.PropertyDescriptions["TestProp2"] );
            
            Assert.AreEqual( 1, lSpec.RequiredProperties.Count );
            Assert.AreEqual( typeof(TimeSpan), lSpec.RequiredProperties["TestProp2"] );
        }

        [Test]
        public void testDefaultInstantiation()
        {
            IPartSpecification lSpec = PartSpecification.createPartSpecification( typeof(SpecTestPart1) );

            var lTestFeatures = new Dictionary<string, object>()
                {
                    { "Object", "Test String Feature" },
                    { "OptionalObject", 84 }
                };

            var lTestProperties = new Dictionary<string, object>()
                {
                    { "TestProp", "16.1" },
                    { "TestProp2", "11:25:54" }
                };

            object lInstanceObject = lSpec.instantiatePart(lTestFeatures, lTestProperties);

            Assert.IsInstanceOf<SpecTestPart1>( lInstanceObject );

            SpecTestPart1 lInstance = (SpecTestPart1)lInstanceObject;

            Assert.AreEqual( "Test String Feature", lInstance.WrappedObject );
            Assert.IsFalse( lInstance.ExecutedNamedConstructor );
            Assert.AreEqual( 84, lInstance.Feature2 );
            Assert.AreEqual( 16.1, lInstance.TestProp );
            Assert.AreEqual( TimeSpan.Parse("11:25:54"), lInstance.TestProp2 );
        }

        [Test]
        public void testNamedInstantiation()
        {
            IPartSpecification lSpec = PartSpecification.createPartSpecification( typeof(SpecTestPart1) );

            var lTestFeatures = new Dictionary<string, object>()
            {
                { "DifferentObject", "Different Test String Feature" }
            };

            var lTestProperties = new Dictionary<string, object>()
            {
                { "TestProp", "16.1" },
                { "TestProp2", "11:25:54" }
            };

            object lInstanceObject = lSpec.instantiatePart("TestNamedConstructor", lTestFeatures, lTestProperties);

            Assert.IsInstanceOf<SpecTestPart1>( lInstanceObject );

            SpecTestPart1 lInstance = (SpecTestPart1)lInstanceObject;

            Assert.AreEqual( "Different Test String Feature", lInstance.WrappedObject );
            Assert.IsTrue( lInstance.ExecutedNamedConstructor );
            Assert.AreEqual( default(int), lInstance.Feature2 );
            Assert.AreEqual( 16.1, lInstance.TestProp );
            Assert.AreEqual( TimeSpan.Parse("11:25:54"), lInstance.TestProp2 );
        }

        [Test]
        public void testReadOnlyProperty()
        {
            Assert.Throws<InvalidPartSpecificationException>( () => PartSpecification.createPartSpecification( typeof(SpecTestPart_ReadOnlyProperty) ) );
        }

        [Test]
        public void testUnmarkedFeature()
        {
            Assert.Throws<InvalidPartSpecificationException>( () => PartSpecification.createPartSpecification( typeof(SpecTestPart_UnmarkedFeature) ) );
        }

        [Test]
        public void testNoPartConstructor()
        {
            Assert.Throws<InvalidPartSpecificationException>( () => PartSpecification.createPartSpecification( typeof(SpecTestPart_NoPartConstructor) ) );
        }

        [Test]
        public void testMultipleDefaultPartConstructors()
        {
            Assert.Throws<InvalidPartSpecificationException>( () => PartSpecification.createPartSpecification( typeof(SpecTestPart1_MultipleDefaultConstructors) ) );
        }

        [Test]
        public void testDuplicateNamedPartConstructors()
        {
            Assert.Throws<InvalidPartSpecificationException>( () => PartSpecification.createPartSpecification( typeof(SpecTestPart1_DuplicateNamedConstructors) ) );
        }

        [Test]
        public void testCreateSpecNullType()
        {
            Assert.Throws<ArgumentNullException>( () => PartSpecification.createPartSpecification( null ) );
        }

        [Test]
        public void testNotAPart()
        {
            Assert.Throws<InvalidPartSpecificationException>( () => PartSpecification.createPartSpecification( typeof(SpecTestPart_NotAPart) ) );
        }

        [Test]
        public void testInstantiate_GenericDef()
        {
            IPartSpecification lSpec = PartSpecification.createPartSpecification( typeof(SpecTestPart1_Generic<>) );
            Assert.Throws<InvalidOperationException>( () => lSpec.instantiatePart( null, null ) );
        }

        [Test]
        public void testInstantiate_NullParams()
        {
            bool lThrewException = false;

            IPartSpecification lSpec = PartSpecification.createPartSpecification( typeof(SpecTestPart1) );

            try
            {
                lSpec.instantiatePart( null, null );
            }
            catch( AggregateException lException )
            {
                lThrewException = true;
                Assert.AreEqual( 2, lException.InnerExceptions?.Count );
            }

            Assert.IsTrue( lThrewException );
        }

        [Test]
        public void testInstantiate_SetterThrows()
        {
            bool lThrewException = false;

            IPartSpecification lSpec = PartSpecification.createPartSpecification( typeof(SpecTestPart1_SetterThrows) );

            try
            {
                var lFeatures = new Dictionary<string, object>();
                
                var lProps = new Dictionary<string, object>();
                lProps["TestProp"] = "84.0";

                lSpec.instantiatePart( lFeatures, lProps );
            }
            catch( AggregateException lException )
            {
                lThrewException = true;
                Assert.AreEqual( 1, lException.InnerExceptions?.Count );
            }

            Assert.IsTrue( lThrewException );
        }

        [Test]
        public void testInstantiate_ConstructorThrows()
        {
            IPartSpecification lSpec = PartSpecification.createPartSpecification( typeof(SpecTestPart1_ConstructorThrows) );

            var lFeatures = new Dictionary<string, object>();
            var lProps = new Dictionary<string, object>();

            Assert.Throws<FailedPartInstantiationException>( () => { lSpec.instantiatePart( lFeatures, lProps ); } );
        }

        [Test]
        public void testInstantiate_PropertiesSetThrows()
        {
            IPartSpecification lSpec = PartSpecification.createPartSpecification( typeof(SpecTestPart1_PropertiesSetThrows) );

            var lFeatures = new Dictionary<string, object>();
            var lProps = new Dictionary<string, object>();

            Assert.Throws<InvalidOperationException>( () => { lSpec.instantiatePart( lFeatures, lProps ); } );
        }

        [Test]
        public void testInstantiate_MissingRequired()
        {
            bool lThrewException = false;

            IPartSpecification lSpec = PartSpecification.createPartSpecification( typeof(SpecTestPart1) );

            try
            {
                var lFeatures = new Dictionary<string, object>();
                var lProps = new Dictionary<string, object>();

                lSpec.instantiatePart( lFeatures, lProps );
            }
            catch( AggregateException lException )
            {
                lThrewException = true;
                Assert.AreEqual( 2, lException.InnerExceptions?.Count );

                var lMissingThings = lException.InnerExceptions?.OfType<MissingFeatureOrPropertyException>();
                Assert.AreEqual( 2, lMissingThings?.Count() ?? 0 );
            }

            Assert.IsTrue( lThrewException );
        }

        [Test]
        public void testInstantiate_Mismatches()
        {
            bool lThrewException = false;

            IPartSpecification lSpec = PartSpecification.createPartSpecification( typeof(SpecTestPart1) );

            try
            {
                var lFeatures = new Dictionary<string, object>()
                {
                    {"Object", null},
                    {"OptionalObject", "not an int"}
                };

                var lProps = new Dictionary<string, object>()
                {
                    {"TestProp", "not a double"},
                    {"TestProp2", null }
                };

                lSpec.instantiatePart( lFeatures, lProps );
            }
            catch( AggregateException lException )
            {
                lThrewException = true;
                Assert.AreEqual( 4, lException.InnerExceptions?.Count );

                var lTypeMismatches = lException.InnerExceptions?.OfType<TypeMismatchException>();
                var lArgNulls = lException.InnerExceptions?.OfType<ArgumentNullException>();

                Assert.AreEqual( 2, lTypeMismatches?.Count() ?? 0 );
                Assert.AreEqual( 2, lArgNulls?.Count() ?? 0 );
            }

            Assert.IsTrue( lThrewException );
        }

        [Test]
        public void testGetSpecs_BadParts()
        {
            bool lThrewException = false;

            try
            {
                // This assembly has bad specs for testing stuff. So this will work.
                // Testing "good" is trickier.
                PartSpecification.getSpecifications( this.GetType().Assembly, out var lExceptions );

                Assert.IsNotNull( lExceptions );
            }
            catch( Exception )
            {
                lThrewException = true;
            }

            Assert.IsFalse( lThrewException );
        }

        [Test]
        public void testGetSpecs_NullArg()
        {
            Assert.DoesNotThrow( () => PartSpecification.getSpecifications( (Assembly)null, out var _ ) );
            Assert.Throws<ArgumentNullException>( () => PartSpecification.getSpecifications( (IEnumerable<Assembly>)null, out var _ ) );
        }

        [Test]
        public void testGetSpecs_GoodParts()
        {
            var lGoodParts = CompileTestParts.getGoodPartsAssembly();

            var lSpecs = PartSpecification.getSpecifications( lGoodParts, out var lExceptions );

            Assert.IsNotNull( lSpecs );
            Assert.IsNull( lExceptions );
        }

        [Test]
        public void testTypeDefinition_ToString()
        {
            var lTD = TypeDefinition.DefinitionFromType( typeof(Dictionary<string, List<int>>) );

            Assert.AreEqual("Dictionary`2<TKey=String,TValue=List`1<T=Int32>>", lTD.ToString() );
        }
    }
}
