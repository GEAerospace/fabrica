// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using GEAviation.Fabrica.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// this file is NOT part of the normal test project!
// It's meant to be set to "Embedded Resource" and pulled in as
// a string. 

// It's compiled into an isolated Assembly at runtime so that
// we have a test assembly that only has good parts in it.

namespace Fabrica.Test
{
    [Part]
    public class SimpleTestPart
    {
        [PartConstructor]
        public SimpleTestPart() {}
    }

    [Part]
    public class SimpleTestPartWithFeature
    {
        public object Feature { get; private set; }

        [PartConstructor]
        internal SimpleTestPartWithFeature( [Feature( "Object" )] object aFeature )
        {
            Feature = aFeature;
        }
    }

    [Part]
    public class SimpleTestPartWithCollections
    {
        public IList<object> List { get; private set; }
        public IDictionary<string, object> Dictionary { get; private set; }

        [PartConstructor]
        internal SimpleTestPartWithCollections( [Feature( "List" )] IList<object> aList, [Feature( "Dictionary" )] IDictionary<string, object> aDictionary )
        {
            List = aList;
            Dictionary = aDictionary;
        }
    }

    [Part]
    public class SimpleTestPartWithList
    {
        public IList<object> List { get; private set; }

        [PartConstructor]
        internal SimpleTestPartWithList( [Feature( "List" )] IList<object> aList )
        {
            List = aList;
        }
    }

    [Part]
    public class SimpleTestPartWithFeaturesAndProperties
    {
        public object Feature { get; private set; }

        [Property]
        public TimeSpan Timeout { get; private set; }

        [PartConstructor]
        public SimpleTestPartWithFeaturesAndProperties( [Feature( "Object" )] object aFeature )
        {
            Feature = aFeature;
        }
    }

    [Part]
    public class SimpleTestPartWithMultipleConstructors
    {
        public object Feature { get; private set; }

        public bool DifferentConstructor { get; private set; }

        [Property]
        public TimeSpan Timeout { get; private set; }

        [PartConstructor]
        public SimpleTestPartWithMultipleConstructors( [Feature( "Object" )] object aFeature )
        {
            Feature = aFeature;
            DifferentConstructor = false;
        }

        [PartConstructor("DifferentConstructor")]
        public SimpleTestPartWithMultipleConstructors( [Feature( "Object" )] decimal aFeature )
        {
            Feature = aFeature;
            DifferentConstructor = true;
        }
    }

    [Part, PartLocator("test-scheme")]
    public class SimpleTestPartLocator : IPartLocator
    {
        [PartConstructor]
        public SimpleTestPartLocator( )
        { }

        public object getPartFromUri(Uri aPartUri)
        {
            if( aPartUri.ToString().Contains( "decimal" ) )
            {
                return 54.0m;
            }
            else if( aPartUri.ToString().Contains( "regex" ) )
            {
                return new Regex( ".*" );
            }
            else if( aPartUri.ToString().Contains( "string" ) )
            {
                return "this is a string";
            }

            return "fake-part " + aPartUri.ToString();
        }
    }

    [Part]
    public class PropertyUriTestPart
    {
        [PartConstructor]
        public PropertyUriTestPart() {}

        [Property]
        public decimal DecimalProp { get; set; }

        [Property]
        public Regex RegexProp { get; set; } 

        [Property]
        public string StringProp { get; set; }
    }

    [Part]
    public class FeatureFromStringTestPart
    {
        [PartConstructor]
        public FeatureFromStringTestPart( [Feature( "Decimal" )] decimal aDecimal )
        {
            DecimalProp = aDecimal;
        }

        public decimal DecimalProp { get; set; }
    }
}
