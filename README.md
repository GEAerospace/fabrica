# Fabrica
Fabrica is a runtime Type Composition/Dependency Injection system for .NET-based applications.

## Introduction
The intent of Fabrica is to provide a fully-configurable dependency injection system, while minimizing
boilerplate code used to operate in such an environment. Goals include:

* Allow for error and integrity checking as early in the loading process or, when possible, statically (without instantiating any objects).
* Supplement the Fabrica system with an editor UI tool that will allow editing the DI configuration without having to manually modify configuration files.

## Terminology
Fabrica uses a "fabrication/manufacturing/CAD" metaphor for most of the terminology. The following are the primary terms,
which will be discussed at length further in the document.

| Term | Description |
| ---- | ----------- |
| Part | The primary unit of functionality within Fabrica. Parts are developer-defined classes, marked with special Attributes, that Fabrica will manage. |
| Feature | Features are the objects needed in order to construct a particular part. Parts will generally be injected into other parts via a target part's Features. |
| Property | Properties are values that are set on constructed Parts. These are generally value types, like `string`, `decimal`, etc. |
| Part Constructor | A class constructor method that is utilized specifically for building a Part. Features are defined as part of a Part Constructor. |
| Part Specification | An object that describes a Part, it's Features and Properties, and allows for instantiating the part. |
| Blueprint | A description of what parts to construct and how to assemble them. This is generally an XML file, but developers can provide their own Blueprint reader/writer classes. |
| Part Container | The primary dependency container. This is used to assemble the parts of one or more blueprints and provide access to those parts once assembled. |

## Concepts

### Parts
Parts are the focus of Fabrica. Developers using Fabrica will spend most of their time creating and implementing parts to be used
within Fabrica. Parts are later constructed and assembled together (injected into each other) using Blueprints.

#### Creating a Part
A bare minimum part is a class that provides a single parameterless constructor, and has the appropriate class attributes (minimally, 
the `[Part]` attribute on the class and the `[PartConstructor]` attribute on the parameterless constructor, as shown below).

The class itself can can have any access modifier and be nested within other classes. However, the class must be instantiable 
(e.g. not `static` or `abstract`).

```csharp
using GEAviation.Fabrica.Definition;

[Part]
public class ExamplePart
{
    [PartConstructor]
    public ExamplePart() { }
}
```

While this example class is not itself useful, it is a "complete" part, and could be declared and configured within a blueprint.

#### Adding Features to a Part
Most Parts will depend on other Parts. Parts declare their dependencies via Features. Features are constructor parameters
annotated with the `[Feature]` attribute and indicate to Fabrica where Parts can be injected.

Because Fabrica primarily utilizes constructor injection, declaring a feature is as simple as adding it
as a parameter to a Part Constructor. The `Type` of the parameter will control what kinds of Parts can be injected for
that feature. It is _highly_ recommended that all Features be declared with `interface` types, in an effort to promote
fully isolated/replaceable components, however, usage of concrete `class` types is fully supported.

By default, and declared Features are required to be fulfilled. If they are not configured in the Blueprint, or the Part destined 
for that feature cannot be constructed, the construction of the Part will fail. Features can be marked as `Required = false` in order
to make the Feature optional. Any optional Feature that has no configured Part to be injected will be fulfilled with `null`.
As a result, Part Constructors with optional Features should be `null` tolerant for the optional Features.

Part Constructors can contain as many Features as are necessary to fully define the Part. However, _every_ parameter of the
part constructor must be a Feature.

```csharp
using GEAviation.Fabrica.Definition;

[Part]
public class ExamplePart
{
    [PartConstructor]
    public ExamplePart( [Feature("FeatureName")] IExampleInterface aFeature ) { }
}
```
```csharp
using GEAviation.Fabrica.Definition;

[Part]
public class ExamplePart
{
    [PartConstructor]
    public ExamplePart( [Feature("FeatureName")] IExampleInterface aFeature,
                        [Feature("OptionalFeature", Required = false)] IExampleInterface aOptionalFeature ) { }
}
```

The feature's name (`FeatureName` or `OptionalFeature` in this example) is how the Feature is referenced in Blueprints.

#### Intermediate Feature Concepts
It's also possible to use `IList<...>` or `IDictionary<string,...>` as the `Type` for a feature. Features declared with
one of these two collection interfaces can accept Part Lists or Part Dictionaries, respectively (both concepts are 
discussed later in this document). These are collections of Parts that can be declared in a Blueprint. This reduces the
need for "plumbing" Parts that only serve to support the construction of more complex dependency trees.

```csharp
using GEAviation.Fabrica.Definition;

[Part]
public class ExamplePart
{
    [PartConstructor]
    public ExamplePart( [Feature("FeatureName")] IList<IExampleInterface> aFeature ) { }
}
```

In this example, the feature can be fulfilled by declaring a Part List in the Blueprint with Parts of the List's element type 
(e.g. `IExampleInterface`). Alternatively, the feature can be fulfilled by a single part that implements the
entire Feature type (e.g. `IList<IExampleInterface>`).

#### Adding Properties to a Part
Parts can be made more configurable by utilizing Properties. These are everyday .NET Properties decorated with 
a `Property` attribute, with a setter. Only properties with setters can be used. The accessibility of the setter
doesn't matter, and this can be used to the developer's advantage to prevent post-construction modification
of these properties, but still allow Fabrica to fill in the details.

Properties should generally be primitive types (e.g. `string`, `decimal`, `int`), or any type that has a well
understood conversion from `string` to the property type, like `enum` types.

Properties, unlike Features, are optional by default. It's up to the developer to provide a good default. Like
features, the optionality of a property can be modified to make it required, via `Required = true` in the `[Property]`
attribute.

```csharp
using GEAviation.Fabrica.Definition;

[Part]
public class ExamplePart
{
    [PartConstructor]
    public ExamplePart( [Feature("FeatureName")] IExampleInterface aFeature ) { }

    [Property]
    public string AwesomeProperty { get; private set; }

    [Property(Required = true)]
    private TimeSpan AnotherProperty { get; internal set; }
}
```

#### Properties Set Notification
If your Part needs to do some initialization after construction, and after the properties have been set,
have the Part implement the `IPropertiesSetNotification` interface. For parts that do not
have Fabrica properties, this notification will still be fired.

```csharp
using GEAviation.Fabrica.Definition;

[Part]
public class ExamplePart : IPropertiesSetNotification
{
    [PartConstructor]
    public ExamplePart( [Feature("FeatureName")] IExampleInterface aFeature ) { }

    [Property]
    public string AwesomeProperty { get; private set; }

    [Property(Required = true)]
    private TimeSpan AnotherProperty { get; internal set; }

    void IPropertiesSetNotification.propertiesSet()
    {
        // Do work after the properties are set.
    }
}
```

#### Multiple Part Constructors
Parts can have multiple `[PartConstructor]` constructors, but can only have one "nameless" Part Constructor.
Additional Part Constructors need to be named using the appropriate constructor of the `[PartConstructor]` attribute.

Each Part Constructor can define a unique set of Features, distinct from the other Part Constructors. However,
all of the rules for Part Constructors still apply. Each parameter of the Part Constructor must be marked as a feature.
They may share the same names as the features in other constructors, however the signature of the constructor must be 
unique (C# requirement).

Named Part Constructors can be called out in the Blueprint, and the alternate set of Features provided.

```csharp
[Part]
public class ExamplePart
{
    [PartConstructor]
    public ExamplePart( [Feature("FeatureName")] IExampleInterface aFeature ) { }

    [PartConstructor("AlternateConstructor")]
    public ExamplePart( [Feature("FeatureName")] IDifferentInterface aFeature ) { }

    [PartConstructor("AnotherConstructor")]
    public ExamplePart( [Feature("WeirdFeature")] IWeirdInterface aFeature ) { }
}
```

Part Constructors may have any access modifier, provided it's not a `static` constructor.

#### Part Locators
A more advanced Part type is the Part Locator. Part Locators serve as factories for parts. They expose
a particular URI scheme (e.g. the `http` in `http://`). These Part Locators are responsible for fulfilling
requests for Parts based on a particular URI. What the URI must contain and what parts get constructed and
provided for that URI is fully determined by the developer's Part Locator implementation.

Part Locators are useful for scenarios where you have a category of parts that are easily identifiable
by URI and are deterministically associated with that URI (i.e. if the same URI is requested multiple times,
the exact same Part is provided (although it does not have to be the same _instance_).

Part Locators are just like normal parts. They can have properties, features, and multiple constructors. They differ
by adding an additional attrbiute to the class (`[PartLocator]`) and implementing the `IPartLocator` interface. Part
Locators must do both in order to be recognized by Fabrica.

```csharp
// This locator will handle any URI that starts with "scheme:"
[Part, PartLocator("scheme")]
public class ExamplePart : IPartLocator
{
    [PartConstructor]
    public ExamplePart( [Feature("FeatureName")] IExampleInterface aFeature ) { }

    object IPartLocator.getPartFromUri( Uri aPartUri )
    {
        // Analyze the URI and return an object if the part with
        // that URI is "located". Part Locators can construct any
        // object they desire, and are completely responsible for
        // said construction.
    }
}
```

### Blueprints
Blueprints are files that describe what parts to create and how to assemble them together. Fabrica provides
a way to read/write Blueprints from/to XML files, however developers are free to create their own readers/writers.

Fabrica stores and manipulates blueprint data using a format agnostic class structure. This is implemented by the types in the
`GEAviation.Fabrica.Model` namespace. Developers are free to use these types for creating editors, writing code to manipulate
or analyze the blueprint model, etc.

For the examples in this README, we'll be using the XML format provided by Fabrica.

A Fabrica Blueprint file can contain multiple blueprints. As a result, the "root" node of the Blueprint XML
is a `blueprint-list`.

```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts />
    </blueprint>
</blueprint-list>
```
(Note that the `namespace` attribute is not currently used as intended. Eventually it will be used to isolate names
within each blueprint.)

The XML file above represents the absolute minimum content of a blueprint XML. However, with no parts defined, it will
result in no objects being instantiated.

A blueprint is a manifest of which parts to construct at runtime, as well as how to fulfill the Feature and
Property needs for each of those Parts.

#### Basic Blueprint Example
Using the first class in this README, we'll construct a single part using blueprint XML.

```csharp
using GEAviation.Fabrica.Definition;

namespace MyCompany.ExampleNamespace
{
    [Part]
    public class ExamplePart
    {
        [PartConstructor]
        public ExamplePart() { }
    }
}
```

```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part name="MyPartInstanceName" id="7BADAB68-69E0-4E0B-9B07-637C0A727C64">
                <runtime-type fullname="MyCompany.ExampleNamespace.ExamplePart" />
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

This XML, when loaded and assembled, will construct a single `ExamplePart` instance, and that instance
will be assigned the given GUID and Name. Names currently have to be globally unique across all loaded blueprints. 
GUIDs, however, must always be unique across all blueprints.

Neither Name or ID are required fields. The intent of these fields is to provide blueprint developers and tools a symbolic way
to later reference a particular instance of a Part. However, being able to omit these is useful if the part instance is nested within 
another part and is not needed by reference anywhere else. If it's not nested within another part, and has no name or ID, it will
still be instantiated (provided it's valid), but will not be retrievable from the Part Container.

```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part name="MyPartInstanceName" id="7BADAB68-69E0-4E0B-9B07-637C0A727C64">
                <runtime-type fullname="MyCompany.ExampleNamespace.ExamplePart" />
            </part>
            <part name="AnotherPartInstanceName" id="09700AB2-5975-40FA-8054-4D1DE7B09641">
                <runtime-type fullname="MyCompany.ExampleNamespace.ExamplePart" />
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

The above example creates two instances of the Example Part.

```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part name="MyPartInstanceName" id="7BADAB68-69E0-4E0B-9B07-637C0A727C64">
                <runtime-type fullname="MyCompany.ExampleNamespace.ExamplePart" />
            </part>
        </parts>
    </blueprint>
    <blueprint namespace="DifferentNamespace">
        <parts>
            <part name="AnotherPartInstanceName" id="09700AB2-5975-40FA-8054-4D1DE7B09641">
                <runtime-type fullname="MyCompany.ExampleNamespace.ExamplePart" />
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

The above example creates the same two instance, but places them in different blueprints.

#### Configuration of Features
The power of dependency injection, and Fabrica, comes from linking multiple parts together. To do this,
define a part that has features that need Parts, define parts that fulfill those needs, and link them together.

The following two classes will be used for this example.

```csharp
using GEAviation.Fabrica.Definition;

namespace MyCompany.ExampleNamespace
{
    [Part]
    public class ExamplePart
    {
        [PartConstructor]
        public ExamplePart([Feature("AnotherPart")] IIsolatingInterface aPart) { }
    }

    [Part]
    public class ExampleSecondPart : IIsolatingInterface
    {
        [PartConstructor]
        public ExampleSecondPart() { }
    }
}
```

(Note, GUIDs may be duplicated from one example to the next, but should always be generated new when actually creating
a part in a blueprint.)

```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part id="7BADAB68-69E0-4E0B-9B07-637C0A727C64">
                <runtime-type fullname="MyCompany.ExampleNamespace.ExamplePart" />
                <features>
                    <feature key="AnotherPart">
                        <id-ref id="09700AB2-5975-40FA-8054-4D1DE7B09641" />
                    </feature>
                </features>
            </part>
            <part id="09700AB2-5975-40FA-8054-4D1DE7B09641">
                <runtime-type fullname="MyCompany.ExampleNamespace.ExampleSecondPart" />
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

In this example, there are two parts declared, of different types. The `runtime-type` element describes the type 
of Part to construct using the fully-qualified .NET type name of the desired Part type.

The first Part in this example, which is an `ExamplePart`, has one feature named `AnotherPart`, which is defined 
in the class with the type `IIsolatingInterface`. To provide a part to "fill" this feature, a reference to another 
part is made, in this case by ID. This is done with an `id-ref` element, with an `id` of the part that should fill in 
this feature.

The second part, a `ExampleSecondPart` (which implements the `IIsolatingInterface` interface) is defined. It has
no features or properties, so that's the entirety of the declaration.

When assembled (described later), two parts will be instantiated, and the relevant features will be filled.

Note that the part that _needs_ the other is declared first. This demonstrates that declaration order within the
blueprint doesn't matter. Fabrica automatically analyzes the references and connections between parts and generates 
a dependency graph to determine what order to construct parts.

#### Part References
In the previous example, a feature is filled with a reference to another part via `id-ref`. There are 3 other 
ways to fill in a feature, and all 4 ways are discussed here.

##### ID References
An ID reference refers to another part in the blueprint (or set of blueprints) by its GUID. If that part exists
within one of the blueprints and is assignable to the type of the target feature, then Fabrica will inject it
into the feature when the target part is constructed.

```xml
<id-ref id="GUID" />
```

##### Name References
A Name reference works exactly like an ID reference, except that it refers to the part by it's name instead.

```xml
<name-ref name="partname" />
```

Since names are not required for parts in a blueprint, you may have to add it to parts that you want to refer
to by name.

```xml
<part name="partname" id="B8B5B52B-FB03-400A-917B-0D6CD781955A">
    <!-- Part Contents -->
</part>
```

##### Part Definitions instead of References
If you need to fill a feature with a part that won't be used anywhere else, you can directly declare
it in the feature instead of using a reference.

The following is the same as the example from earlier, except not using a reference.
```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part id="7BADAB68-69E0-4E0B-9B07-637C0A727C64">
                <runtime-type fullname="MyCompany.ExampleNamespace.ExamplePart" />
                <features>
                    <feature key="AnotherPart">
                        <part id="09700AB2-5975-40FA-8054-4D1DE7B09641">
                            <runtime-type fullname="MyCompany.ExampleNamespace.ExampleSecondPart" />
                        </part>
                    </feature>
                </features>
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

While the part is now declared in-place, parts can still refer to it by using any valid reference.

##### URI References
If, instead of using a part declared directly in the blueprint, you'd like to include a part that is
provided by a part locator, you can use a URI reference. To use a URI reference, you declare a
`uri-ref` element with the appropriate `uri` attribute. In order for this to work, a Part Locator must
be declared in the blueprint supporting the scheme of the URI (discussed later).

```xml
<uri-ref uri="scheme:/rest/of/the/uri" />
```

#### Part Properties
When defining a part that requires/uses properties, use the `properties` element. This element works
similarly to the `features` element, however it does not take parts but rather simple string values.

```csharp
using GEAviation.Fabrica.Definition;

namespace MyCompany.ExampleNamespace
{
    [Part]
    public class PartWithProperties
    {
        [PartConstructor]
        public ExampleSecondPart() { }

        [Property]
        public string MyProperty { get; set; }

        [Property(Required = true)]
        protected decimal MyDecimal { get; set; }
    }
}
```
```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part id="3C395CCC-9D06-4A66-A13F-63C95917BE2D">
                <runtime-type fullname="MyCompany.ExampleNamespace.PartWithProperties" />
                <properties>
                    <property key="MyProperty" value="some string value" />
                    <property key="MyDecimal" value="14.5" />
                </properties>
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

In the above example a part with properties, `PartWithProperties`, was defined in the blueprint
and both of its properties were filled with a value. The property values are specified
with strings, but they will be converted to the appropriate property type, provided a string 
conversion exists _to_ that type (which is true for both `string` and `decimal`).

#### Part Locators
Part Locators are declared in a blueprint exactly like any other part except that an additional 
attribute, `part-locator-scheme`, is required in order for Fabrica to resolve any `uri-ref` references
that depend on that locator.

```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part id="A359F8C9-FD39-4CC5-A362-6A059EC92E5E" part-locator-scheme="scheme">
                <runtime-type fullname="MyCompany.ExampleNamespace.SomeLocatorPart" />
                <!-- Other Features/Properties -->
            </part>
            <part id="7BADAB68-69E0-4E0B-9B07-637C0A727C64">
                <runtime-type fullname="MyCompany.ExampleNamespace.ExamplePart" />
                <features>
                    <feature key="AnotherPart">
                        <uri-ref uri="scheme:/rest/of/the/uri" />
                    </feature>
                </features>
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

In the previous example, a part locator of type `SomeLocatorPart` was declared, its URI scheme
declared as `scheme` (but this could be any valid URI scheme chosen by the developer of the locators)
and another part in the same blueprint is requesting a part from that locator via a `uri-ref`.

Any number of part locators can be declared, and they can refer to other part locators (but not circularly).
The only restriction is that only one part locator may be declared for any single URI scheme.

### Intermediate Blueprint Concepts
Blueprints supports some extra features that may not be needed but support outlier uses of Fabrica.

#### Property URI Values
It's possible to specify a `uri` instead of a `value` for a property. This will instead request the
value of that property from a part locator. If the locator provides a string, Fabrica will attempt to
convert that string to the target property's type. The same string-to-property conversion rules that exist
for properties fulfilled by value still apply, however if the part locator supplies a type other than string, 
and that type can be converted to the target property's type it will be converted. 
This allows for Properties to consist of more complex types.

```csharp
using GEAviation.Fabrica.Definition;

namespace MyCompany.ExampleNamespace
{
    [Part]
    public class PartWithProperties
    {
        [PartConstructor]
        public PartWithProperties() { }

        [Property]
        public string MyProperty { get; set; }

        [Property(Required = true)]
        protected decimal MyDecimal { get; set; }
    }
}
```
```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part id="3C395CCC-9D06-4A66-A13F-63C95917BE2D">
                <runtime-type fullname="MyCompany.ExampleNamespace.PartWithProperties" />
                <properties>
                    <property key="MyProperty" value="some string value" />
                    <property key="MyDecimal" uri="scheme:/uri/that/will/give/some/decimal/value" />
                </properties>
            </part>
            <part id="A359F8C9-FD39-4CC5-A362-6A059EC92E5E" part-locator-scheme="scheme">
                <runtime-type fullname="MyCompany.ExampleNamespace.SomeLocatorPart" />
                <!-- Other Features/Properties -->
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

#### Constant Feature Values
For features whose type is a value type that has a known conversion from a string, the value can be
declared using the `<constant/>` tag in the blueprint file. This makes it possible to use simple data types
in features rather than only parts or objects retrieved from a part locator.

```csharp
using GEAviation.Fabrica.Definition;

namespace MyCompany.ExampleNamespace
{
    [Part]
    public class PartWithSimpleFeature
    {
        [PartConstructor]
        public PartWithSimpleFeature( [Feature("SomeTimeSpan")] TimeSpan aTimeSpan ) { }
    }
}
```
```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part id="3C395CCC-9D06-4A66-A13F-63C95917BE2D">
                <runtime-type fullname="MyCompany.ExampleNamespace.PartWithSimpleFeature" />
                <features>
                    <feature key="SomeTimeSpan">
                        <constant value="01:23:14"/>
                    </feature>
                </features>
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```
In this example, the `<constant value="01:23:14"/>` is automatically converted from the string
representation of a `TimeSpan` to an actual `TimeSpan` value and passed to the part during construction.

#### Part Lists
For more complex scenarios, Fabrica provides the ability to declare Part Lists within a blueprint. 

Part Lists are declared using the `<part-list/>` element. Within the `<part-list/>` element, 
any number of parts and part references can be added, provided each of those objects are compatible with
the element type (in the example below this is `object`).

##### With an Interface as the Runtime Type
If declaring the feature with an interface type, the receiving feature must have the explicit type `IEnumerable<>`, 
`ICollection<>` or `IList<>`. In this scenario, Fabrica will automatically generate a `List<T>` instance compatible
with the feature.

##### With a concrete Class as the Runtime Type
If declaring the feature with a concrete type, the type of the feature must implement `ICollection<T>` and
have a parameterless public constructor. Fabrica will automatically instantiate the type and use 
`ICollection<T>.Add(T)` to add parts to the collection.

For Part Lists, the order that the "sub-parts" are declared is the order that Fabrica adds them to the 
runtime collection.

```csharp
using GEAviation.Fabrica.Definition;

namespace MyCompany.ExampleNamespace
{
    [Part]
    public class PartWithList
    {
        [PartConstructor]
        public PartWithList( [Feature("SomeList")] IList<object> aList ) { }
    }
}
```
```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part id="3C395CCC-9D06-4A66-A13F-63C95917BE2D">
                <runtime-type fullname="MyCompany.ExampleNamespace.PartWithList" />
                <features>
                    <feature key="SomeList">
                        <part-list>
                            <!-- Reference/declare any number of other parts here -->
                            <part>
                                <!-- ... -->
                            </part>
                            <name-ref ... />
                            <id-ref ... />
                            <uri-ref ... />
                        </part-list>
                    </feature>
                </features>
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```
Additionally, to make lists reusable, they can be declared as a stand-alone part and reused throughout
the blueprint like any other part. The only caveat is that a `<runtime-type/>` or `<runtime-type-alias/>`
element must be declared first to instruct Fabrica on what data type the list holds. This runtime type must follow
the same rules as described in the _With an Interface as the Runtime Type_ and _With a concrete Class as the Runtime Type_ 
sections above.

Just like with normal parts, reusable part lists must have a type that is compatible with any feature
it's referenced from.

```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part-list name="reusable-list">
                <runtime-type fullname="System.Collections.Generic.IList`1">
                    <type-param param-name="T" fullname="System.Object"/>
                </runtime-type>
                <!-- Reference/declare any number of other parts here -->
                <part>
                    <!-- ... -->
                </part>
                <name-ref ... />
                <id-ref ... />
                <uri-ref ... />
            </part-list>
            <part id="3C395CCC-9D06-4A66-A13F-63C95917BE2D">
                <runtime-type fullname="MyCompany.ExampleNamespace.PartWithList" />
                <features>
                    <feature key="SomeList">
                        <name-ref name="reusable-list"/>
                    </feature>
                </features>
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```
The following example is nearly identical to the previous example, but the blueprint is explicitly declaring
to use a `HashSet<T>` instead of a `IList<T>`. Since `HashSet<T>` implements `ICollection<T>`, and `HashSet<T>`
has a public parameterless constructor, Fabrica can properly handle instantiating and populating the collection.
```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part-list name="reusable-list">
                <runtime-type fullname="System.Collections.Generic.HashSet`1">
                    <type-param param-name="T" fullname="System.Object"/>
                </runtime-type>
                <!-- Reference/declare any number of other parts here -->
                <part>
                    <!-- ... -->
                </part>
                <name-ref ... />
                <id-ref ... />
                <uri-ref ... />
            </part-list>
            <part id="3C395CCC-9D06-4A66-A13F-63C95917BE2D">
                <runtime-type fullname="MyCompany.ExampleNamespace.PartWithList" />
                <features>
                    <feature key="SomeList">
                        <name-ref name="reusable-list"/>
                    </feature>
                </features>
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

#### Part Dictionaries
In addition to Part Lists, Fabrica provides the ability to declare Part Dictionaries within
the blueprint. 

Part Dictionaries are declared using the `<part-dictionary/>` element. Within the `<part-dictionary/>` element, 
any number of parts and part references can be added, provided each of those objects are compatible with
the dictionary value type (in the example below this is `object`). The key type must be `string`.

##### With an Interface as the Runtime Type
If declaring the receiving feature with an interface type, the receiving feature must have the explicit type 
`IDictionary<string,T>`, `IEnumerable<KeyValuePair<string,T>>` or `ICollection<KeyValuePair<string,T>>`, where
`T` is the desired element type. In this scenario, Fabrica will automatically generate a `Dictionary<string,T>` 
instance compatible with the feature.

##### With a concrete Class as the Runtime Type
If declaring the feature with a concrete class, the type of the feature must implement `ICollection<KeyValuePair<string,T>>` and
have a parameterless public constructor. Fabrica will automatically instantiate the type and use 
`ICollection<KeyValuePair<string,T>>.Add(KeyValuePair<string,T>)` to add parts to the collection.

For Part Dictionaries, the order that the "sub-parts" are declared in is the order that Fabrica adds them to the 
runtime collection.

```csharp
using GEAviation.Fabrica.Definition;

namespace MyCompany.ExampleNamespace
{
    [Part]
    public class PartWithDictionary
    {
        [PartConstructor]
        public PartWithDictionary( [Feature("SomeDictionary")] IDictionary<string, object> aDictionary ) { }
    }
}
```
```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part id="3C395CCC-9D06-4A66-A13F-63C95917BE2D">
                <runtime-type fullname="MyCompany.ExampleNamespace.PartWithDictionary" />
                <features>
                    <feature key="SomeDictionary">
                        <part-dictionary>
                            <!-- Reference/declare any number of other parts here, under a key-value element. -->
                            <key-value key="A">
                                <part>
                                    <!-- ... -->
                                </part>
                            </key-value>
                            <key-value key="B">
                                <name-ref ... />
                            </key-value>
                            <key-value key="C">
                                <id-ref ... />
                            </key-value>
                            <key-value key="D">
                                <uri-ref ... />
                            </key-value>
                        </part-dictionary>
                    </feature>
                </features>
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```
Additionally, like part lists, part dictionaries can be declared as a stand-alone part and reused throughout
the blueprint like any other part. As with standalone part lists, a `<runtime-type/>` or `<runtime-type-alias/>`
element must be declared first to instruct Fabrica on what data type the dictionary holds. This runtime type must follow
the same rules as described in the _With an Interface as the Runtime Type_ and _With a concrete Class as the Runtime Type_ 
sections above.

Just like with normal parts, reusable part dictionaries must have a type that is compatible with any feature
it's referenced from.

```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part-dictionary name="reusable-dictionary">
                <runtime-type fullname="System.Collections.Generic.IDictionary`2">
                    <type-param param-name="TKey" fullname="System.String"/>
                    <type-param param-name="TValue" fullname="System.Object"/>
                </runtime-type>
                <!-- Reference/declare any number of other parts here, under a key-value element. -->
                <key-value key="A">
                    <part>
                        <!-- ... -->
                    </part>
                </key-value>
                <key-value key="B">
                    <name-ref ... />
                </key-value>
                <key-value key="C">
                    <id-ref ... />
                </key-value>
                <key-value key="D">
                    <uri-ref ... />
                </key-value>
            </part-dictionary>
            <part id="3C395CCC-9D06-4A66-A13F-63C95917BE2D">
                <runtime-type fullname="MyCompany.ExampleNamespace.PartWithDictionary" />
                <features>
                    <feature key="SomeDictionary">
                        <name-ref name="reusable-dictionary"/>
                    </feature>
                </features>
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

#### External Parts
External parts allow for a user of Fabrica to instantiate objects upfront and supply them to Fabrica
when its time to assemble the remainder of the blueprints. This allows for scenarios where it's infeasible
for Fabrica to construct the part or where some parts need to exist for other purposes ahead of assembling
the rest of the parts. This can also be used to provide Fabrica with objects that are _not_ Parts so that
parts that need those objects can still be automatically constructed.

External parts are treated the same as regular parts in a blueprint. However, they need to be declared
at the top level of the blueprint (i.e. they cannot be declared within any tag other than `<parts/>`). 
External parts may _either_ have a name or an ID, but not both. If the external part is a Part Locator, 
the external part declaration must include the `part-locator-scheme` attribute, just as any other part locator 
would be declared.

When assembling blueprints, the external parts must be instantiated ahead of time by the developer and supplied to
the assembly process using the ID or Name it's declared with in the blueprint.

External parts may be referred to like normal parts via `id-ref`, `name-ref` or `uri-ref` references.

```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <external-part name="something-that-already-exists" />

            <part id="7BADAB68-69E0-4E0B-9B07-637C0A727C64">
                <runtime-type fullname="MyCompany.ExampleNamespace.ExamplePart" />
                <features>
                    <feature key="AnotherPart">
                        <name-ref name="something-that-already-exists" />
                    </feature>
                </features>
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

Declaring an external part is required so that fabrica can verify that all necessary parts exist while
analyzing the blueprint and constructing the dependency information.

#### Undefined Parts
Undefined parts are parts that are incomplete. They are used as placeholders for parts that either
need to be defined by a developer or user, or by tools that generate blueprints to keep track of incomplete
parts.

Like External Parts, they must be declared at the top level of a blueprint (i.e. within the `<parts/>` section).
Undefined parts may have either a name, ID, or both. One of the two must be declared.

Undefined parts cannot have any other information.

```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <undefined-part name="something-to-fix-later" />

            <part id="7BADAB68-69E0-4E0B-9B07-637C0A727C64">
                <runtime-type fullname="MyCompany.ExampleNamespace.ExamplePart" />
                <features>
                    <feature key="AnotherPart">
                        <name-ref name="something-to-fix-later" />
                    </feature>
                </features>
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

Like External Parts, undefined parts can be referred to via `id-ref` or `name-ref`. Any part that
refers to an undefined part will be marked as "incomplete" during Fabrica's analysis of the part graph
and dependencies. All parts that refer to undefined or incomplete parts will be excluded from assembly, without
error.

#### Part Metadata
Parts, External Parts and Undefined Parts all allow for custom metadata to be attached. This metadata can
be used by tools reading/writing/generating blueprints to store tool-specific data along with the parts
within the blueprint.

Part metadata is declared as `data` key-value pairs within a `metadata` section in the part declaration.
The value is treated as a string. To store more complex data within the metadata, it's _highly_ recommended
that the XML `CDATA` construct be used to ensure XML parsing is not affected by the metadata values.

```XML
<?xml version="1.0" encoding="utf-8"?>
<blueprint-list xmlns="http://www.geaviation.com/NG/Fabrica">
    <blueprint namespace="MyNamespace">
        <parts>
            <part id="7BADAB68-69E0-4E0B-9B07-637C0A727C64">
                <runtime-type fullname="MyCompany.ExampleNamespace.ExamplePart" />
                <features>
                    <feature key="AnotherPart">
                        <name-ref name="something-to-fix-later" />
                    </feature>
                </features>
                <metadata>
                    <data key="MyTool">
                        <![CDATA[
                        Tool-specific value
                        ]]>
                    </data>
                </metadata>
            </part>
        </parts>
    </blueprint>
</blueprint-list>
```

Its use is completely open to the developer. When a blueprint is loaded or saved, it's metadata will
be retained, provided the appropriate reader/writer retains that data. The default XML reader/writer provided
with Fabrica will retain this data.

### Assembling Parts (The Hard Way)
The steps for assembling parts are as follows:

#### 1. Get a collection of Part Specifications
In order to assemble parts, a collection of `IPartSpecification` objects representing the
various parts available for use within a blueprint is needed.

Fabrica provides some facilities for generating this collection.

##### Manually Find Parts
A developer can use the static method `PartSpecification.createPartSpecification()` to generate an
`IPartSpecification` object from a known `Type`. If that type is a valid `[Part]`, a specification will
be generated. Using this, the developer can choose how to find and collect the parts they care about.

##### Automatically Find Parts
A developer can use the `PartSpecification.getSpecifications()` function to generate a collection
of `IPartSpecification` objects from a provided collection of `System.Assembly` objects. The developer chooses
which .NET Assemblies to provide to this method, and Fabrica will automatically find all of the parts inside 
of those assemblies. 

If any `[Part]` types within the supplied assemblies are not valid, they will not be provided as 
an `IPartSpecification`. `PartSpecification.getSpecifications()` provides an `AggregateException` `out`
parameter than can be used to identify errors.

#### 2. Load Blueprints
Next, the developer must collect one or more `Blueprint` objects to assemble. This can be done by 
generating the blueprint in code (not recommended unless the code is part of a tool for creating/modifying
blueprints). Preferably, this can be done using a Blueprint reader. A blueprint reader is any class
that implements the `IBlueprintListFileReader` interface. 

Fabrica comes with a default implementation of this interface, `GEAviation.Fabrica.Model.IO.XmlBlueprintReader`. 
This can be used to read any valid `blueprint-list` XML file, generating a `Blueprint` object for each 
blueprint contained in the XML.

#### 3. Generate any External Parts
If there are "external" parts that the application wants to provide, they will need to be constructed and
provided via `GEAviation.Fabrica.ExternalPartInstance` objects. The assembly process takes a collection of
these as input, and are used in conjunction with `external-part` declarations in the blueprint. Note that
the information provided when constructing `ExternalPartInstance` objects must match exactly that information
in the blueprint (ID or name, and part locator scheme, if the object is a part locator).

Any object can be used as an external part, not just those implemented as parts. Any class marked with the
`IPartLocator` interface can be used as an external part locator.

#### 4. Create the `PartContainer`
The `PartContainer` class is the class that represents the loaded parts, and the one that does the work
of assembling the parts.

The constructor for a `PartContainer` requires the Part Specifications and Blueprints loaded earlier in the
process. 

After the `PartContainer` object is created, call its `assembleParts` method. If there are external parts to 
provide, one of the overloads of this method will accept them.

When the `assembleParts` call returns, the loaded parts can be accessed by Name, ID or Locator Scheme via the
following properties on the `PartContainer` object:

* `PartLocators`
* `PartsByID`
* `PartsByName`

These properties are all dictionaries, and the parts are available as values within them. They are provided as
`object` instances and it's up to the developer to properly assess their actual `Type` and cast/use as appropriate.

### Assembling Parts (The Easier Way)
To make things easier, Fabrica provides a facade that makes the process a easier.

#### The `FabricaFacade` Facade
To use this facade, you'll need 4 things:

* The path to the file containing the blueprints that should be loaded.
* A collection of `IPartSpecification` objects OR a collection of `System.Assembly` objects.
* Any external parts that should be used.
* The type of a `IBlueprintListFileReader`.

Once those things are ready, it's one function call to get and assemble a `PartContainer`:

```csharp
// With specifications and a Fabrica XmlBlueprintReader. 
PartContainer lMyParts = 
    FabricaFacade.loadPartContainer<XmlBlueprintReader>( @"c:\path\to\file.xml", lSpecification, lExternalParts );
```

```csharp
// With System.Assembly objects and a Fabrica XmlBlueprintReader. 
PartContainer lMyParts = 
    FabricaFacade.loadPartContainer<XmlBlueprintReader>( @"c:\path\to\file.xml", lAssemblies, lExternalParts );
```

### Part Construction Errors
During the assembly process, or when attempting to get `IPartSpecification` objects, a number of Exceptions may be
generated. Fabrica attempts to collect as many of these exceptions as possible, rather than stopping at the first thrown. 
The intent is that this will allow more upfront discovery of problems, and prevent the fix-break loop that can
happen otherwise. As a result, the generated Exception may be a `AggregateException`.
