// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace GEAviation.Fabrica.Model 
{
    /// <summary>
    /// This represents a .NET CLR Type within the Blueprint model. This can
    /// be realized into a <see cref="Type"/>, provided the <see cref="Type"/>
    /// exists within the executing <see cref="AppDomain"/>'s assemblies.
    /// </summary>
    public class TypeDefinition : ITypeDefOrRef
    {
        /// <summary>
        /// The full name of the .NET CLR Type, including Arity.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// The Type Parameters of this TypeDefinition, provided the 
        /// definition represents a generic Type.
        /// </summary>
        public IDictionary<string, TypeDefinition> TypeParameters { get; }

        /// <summary>
        /// Constructs a new, empty TypeDefintion object.
        /// </summary>
        public TypeDefinition()
        {
            TypeParameters = new Dictionary<string, TypeDefinition>();
        }

        /// <summary>
        /// Copy constructor. Generates a deep copy of the specified <see cref="TypeDefinition"/>.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public TypeDefinition(TypeDefinition aToCopy, bool aShallow = false)
            : this()
        {
            FullName = aToCopy.FullName;
            if(!aShallow)
            { 
                foreach(var lTypeParam in aToCopy.TypeParameters)
                {
                    TypeParameters[lTypeParam.Key] = new TypeDefinition(lTypeParam.Value);
                }
            }
        }

        /// <summary>
        /// Overrides <see cref="Object.ToString()"/> to provide a C#-style rendering of
        /// the entire type, including the type parameters (recursively).
        /// </summary>
        /// <returns>
        /// C#-Style rendering of the full type name.
        /// </returns>
        public override string ToString()
        {
            if( !string.IsNullOrWhiteSpace( FullName ) )
            {
                var lNameParts = FullName.Split( '.' );
                var lShortTypeName = lNameParts[lNameParts.Length - 1];

                if( TypeParameters.Count > 0 )
                {
                    // "ParamName=Type" is used instead of just "Type" because TypeDefinition
                    // doesn't force declaration order like C# does and the output of this
                    // function wants to be clear about which Type goes with which type parameters.
                    var lParams = string.Join( ",", TypeParameters.Select( aItem => $"{aItem.Key}={aItem.Value}" ) );
                    lShortTypeName = $"{lShortTypeName}<{lParams}>";
                }

                return lShortTypeName;
            }

            return "UNKNOWN";
        }

        /// <summary>
        /// Generates a <see cref="Type"/> object for the provided <see cref="TypeDefinition"/>
        /// provided that the desired <see cref="Type"/> (and full tree of type arguments)
        /// exists within the current <see cref="AppDomain"/>'s assemblies.
        /// </summary>
        /// <param name="aDefinition">
        /// The <see cref="TypeDefinition"/> to generate a <see cref="Type"/> from.
        /// </param>
        /// <returns>
        /// A valid <see cref="Type"/> for the <see cref="TypeDefinition"/> or null
        /// if the Type or any of it's type parameters could not be resolved.
        /// </returns>
        public static Type TypeFromDefinition( TypeDefinition aDefinition )
        {
            if (aDefinition == null)
            {
                throw new ArgumentNullException(nameof(aDefinition));
            }

            var lAvailableAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            Type getTypeInAssemblies( string aFullName )
            {
                Type lFound = null;
                foreach( var lAssembly in lAvailableAssemblies )
                {
                    lFound = lAssembly.GetType( aFullName );
                    if( lFound != null )
                    {
                        break;
                    }
                }

                return lFound;
            }

            Type internalRecur( TypeDefinition aTypeDef )
            {
                Type lRootType = getTypeInAssemblies( aTypeDef.FullName );

                if( lRootType != null && lRootType.IsGenericTypeDefinition )
                {
                    var lGenericParams = lRootType.GetGenericArguments();

                    List<Type> lParamTypes = new List<Type>();

                    foreach( var lParam in lGenericParams )
                    {
                        if( aTypeDef.TypeParameters.ContainsKey( lParam.Name ) )
                        {
                            var lParamDef = aTypeDef.TypeParameters[lParam.Name];
                            var lParamType = internalRecur( lParamDef );
                            if( lParamType != null )
                            {
                                lParamTypes.Add( lParamType );
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }

                    if( lParamTypes.Count == lGenericParams.Length )
                    {
                        lRootType = lRootType.MakeGenericType( lParamTypes.ToArray() );
                    }
                }

                return lRootType;
            }

            return internalRecur( aDefinition );
        }

        /// <summary>
        /// Generates a <see cref="TypeDefinition"/> from a .NET CLR Type.
        /// </summary>
        /// <param name="aOriginalType">
        /// The .NET CLR <see cref="Type"/> to generate a <see cref="TypeDefinition"/> from.
        /// This cannot be a generic definition (i.e. all type parameters must be filled).
        /// </param>
        /// <returns>
        /// A <see cref="TypeDefinition"/> object representing the original .NET CLR Type.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="aOriginalType"/> argument is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If <paramref name="aOriginalType"/> or any of it's type arguments
        /// are generic definitions (i.e. do not have all type parameters filled).
        /// </exception>
        public static TypeDefinition DefinitionFromType( Type aOriginalType )
        {
            if (aOriginalType == null)
            {
                throw new ArgumentNullException(nameof(aOriginalType));
            }

            if( aOriginalType.IsGenericTypeDefinition )
            {
                throw new InvalidOperationException( "Provided type cannot be generic type definition." );
            }

            TypeDefinition lTypeDef = new TypeDefinition();

            lTypeDef.FullName = aOriginalType.FullName;

            if( aOriginalType.IsGenericType )
            {
                var lGenericDef = aOriginalType.GetGenericTypeDefinition();
                lTypeDef.FullName = lGenericDef.FullName;

                var lGenericParams = lGenericDef.GetGenericArguments();
                var lGenericArgs = aOriginalType.GetGenericArguments();

                for( int lParam = 0; lParam < lGenericParams.Length; lParam++ )
                {
                    string lParamName = lGenericParams[lParam].Name;
                    TypeDefinition lArgTypeDef = DefinitionFromType( lGenericArgs[lParam] );

                    lTypeDef.TypeParameters[lParamName] = lArgTypeDef;
                }
            }

            return lTypeDef;
        }
    }
}
