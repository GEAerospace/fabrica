// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using GEAviation.Fabrica.Definition;
using GEAviation.Fabrica.Model;
using GEAviation.Fabrica.Model.IO;
using GEAviation.Fabrica.Extensibility;
using GEAviation.Fabrica.Utility;
using GEAviation.Fabrica.Structures.GeneralGraph;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GEAviation.Fabrica
{
    /// <summary>
    /// This is the central class of the Fabrica system. It serves as a dependency injection composer and
    /// container for all Parts in the specified Blueprints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Usage of <see cref="PartContainer"/> is 2-3 steps. First, construct a new <see cref="PartContainer"/>
    /// using one of the provided constructors. Then, call the <see cref="PartContainer.assembleParts()"/> method.
    /// Finally, assuming no exception ocurred during assembly, access loaded assembled Parts using the
    /// <see cref="PartsByID"/>, <see cref="PartsByName"/>, or <see cref="PartLocators"/> collections.
    /// </para>
    /// </remarks>
    public class PartContainer : IDisposable
    {

        internal class PartInstantiationTimer : IDisposable
        {
            private StreamWriter mWriter;
            private Stopwatch mStopwatch;
            private Part mFullPart;

            public PartInstantiationTimer( Part aFullPart, StreamWriter aWriter )
            {
                mWriter = aWriter;
                mFullPart = aFullPart;
                mStopwatch = new Stopwatch();
                mStopwatch.Start();
            }

            public void Dispose()
            {
                mStopwatch.Stop();
                var lLocation = "?,?";
                if ( GlobalProperties.FileLocationInfo.hasValue( mFullPart ) )
                {
                    lLocation = GlobalProperties.FileLocationInfo.getValue( mFullPart );
                }
                mWriter.WriteLine( $"{mFullPart.ID},{mFullPart.Name},\"{lLocation}\",{mStopwatch.Elapsed.TotalMilliseconds}" );
            }
        }

        /// <summary>
        /// Represents a single part in the Part Graph. Used to compute dependency and
        /// validate the model.
        /// </summary>
        private class PartGraphNode : GraphNode
        {
            /// <summary>
            /// The Part that this node represents.
            /// </summary>
            public IPart GraphedPart { get; private set; }

            /// <summary>
            /// Used during the dependency graph building step to determine dependency order.
            /// The smaller this number, the earlier this part should be built. Depth = 0 indicates
            /// the part has no dependencies and should be build first.
            /// </summary>
            public int DependencyDepth { get; set; } = 0;

            /// <summary>
            /// Represents the actual <see cref="TypeDefinition"/> for the underlying <see cref="GraphedPart"/>,
            /// after resolving any potential <see cref="TypeAlias"/>.
            /// </summary>
            public TypeDefinition FullTypeDefinition { get; set; }

            /// <summary>
            /// This property indicates if this node has any dependencies on other nodes. The <see cref="GraphedPart"/>
            /// of nodes that return false can be constructed independently of other Parts in the system.
            /// </summary>
            public bool HasDependencies
            {
                get
                {
                    var lOutgoingEdges = this.getEdges<DependsOnRelationship>( SearchDirection.FromThisNode );
                    return lOutgoingEdges.Any();
                }
            }

            /// <summary>
            /// Used during the graph instantiation and validation steps to determine which parts
            /// have fully-defined dependencies. Parts that are "incomplete" (Complete=false), 
            /// cannot be instantiated, and consequently, neither can any of the parts that
            /// depend upon it, directly or indirectly.
            /// </summary>
            public bool Complete { get; set; } = true;

            /// <summary>
            /// Creates a new <see cref="PartGraphNode"/> from the provided <see cref="IPart"/>.
            /// </summary>
            /// <param name="aPart">
            /// The <see cref="IPart"/> that this node will represent.
            /// </param>
            public PartGraphNode( IPart aPart )
                : base()
            {
                GraphedPart = aPart ?? throw new ArgumentNullException( nameof( aPart ) );
                if ( GraphedPart.ID != Guid.Empty )
                {
                    this.Name = GraphedPart.ID.ToString().ToLower();
                }
                else
                {
                    // The node needs a unique ID in order to be in the graph,
                    // but it can be any random number.
                    this.Name = Guid.NewGuid().ToString();
                }
            }
        }

        /// <summary>
        /// The relationship type used for edges in the Fabrica Part Graph.
        /// </summary>
        private class DependsOnRelationship : RelationshipType
        { }

        /// <summary>
        /// The Part Specifications provided during construction of the <see cref="PartContainer"/>.
        /// </summary>
        private readonly IEnumerable<IPartSpecification> mSpecifications;

        /// <summary>
        /// 
        /// </summary>
        private readonly IDictionary<string, CompositePartDef> mComposites;

        /// <summary>
        /// The Blueprints provided during construction of the <see cref="PartContainer"/>.
        /// </summary>
        private readonly IEnumerable<Blueprint> mBlueprints;

        /// <summary>
        /// The "singleton" instance of this relationship used on all of the edges of the Part Graph.
        /// </summary>
        private readonly DependsOnRelationship mDependsOnRelationship = new DependsOnRelationship();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aSpecifications"></param>
        /// <param name="aBlueprints"></param>
        public PartContainer( IEnumerable<IPartSpecification> aSpecifications, IEnumerable<Blueprint> aBlueprints )
        {
            mSpecifications = aSpecifications ?? throw new ArgumentNullException( nameof( aSpecifications ) );
            mBlueprints = aBlueprints ?? throw new ArgumentNullException( nameof( aBlueprints ) );

            mComposites = new Dictionary<string, CompositePartDef>();
            foreach ( var lCompositeDef in aBlueprints.SelectMany( aBlueprint => aBlueprint.Composites ) )
            {
                mComposites[lCompositeDef.Key] = lCompositeDef.Value;
            }

            buildGraph();
        }

        /// <summary>
        /// The graph of Parts in the provided Blueprints. Computed by <see cref="buildGraph"/>.
        /// </summary>
        private Graph<PartGraphNode, GraphEdge> mPartGraph;

        /// <summary>
        /// Computes the Part Graph from the provided Blueprints. Also computes dependency information
        /// and validates the graph (invalidating any parts that, based on "static analysis", cannot be 
        /// constructed).
        /// </summary>
        private void buildGraph()
        {
#if DEBUG
            System.IO.StreamWriter lExpansionLog = null;

            try
            {

                // This code is to help debug composite expansion. This deletes older versions of the
                // expansion log.
                if ( System.IO.File.Exists( "expansion.log" ) )
                {
                    System.IO.File.Delete( "expansion.log" );
                }

                lExpansionLog = new System.IO.StreamWriter( "expansion.log" );
                XmlBlueprintWriter.OutputAllIDs = true;
#endif

                mPartGraph = new Graph<PartGraphNode, GraphEdge>();

                // Round 1: Collect all part/undefined part definitions
                Dictionary<string, PartGraphNode> lNodes = new Dictionary<string, PartGraphNode>();
                Dictionary<string, PartGraphNode> lNamedNodes = new Dictionary<string, PartGraphNode>();
                Dictionary<string, PartGraphNode> lLocatorNodes = new Dictionary<string, PartGraphNode>();


                IPart expandCompositePart( Part aPartToExpand, IDictionary<string, TypeAlias> aTypeAliases )
                {
                    // For composites, perform a tree swap
                    // - Keep ID/Name of part
                    // - Keep metadata of part
                    // - Turn features that are explicitly parts into id-refs to the parts
                    //     - Recusively add these parts to the graph using the addPart call.
                    // - Hold on to property values
                    // - Duplicate the composite defs part tree and give the 
                    //   duplicated root the ID/Name/Metadata of the original part declaration
                    // - For each instance of a feature slot replace with the id-ref of the part originally
                    //   declared with the original part. If the original part was using a name-ref, id-ref, uri-ref
                    //   duplicate it and use the duplicate.
                    // - For each instance of a property slot replace with a duplicate of the property
                    //   value declared for the slot.
                    // - Continue as if nothing happened.
                    Dictionary<string, IPartDefOrRef> lFeatureArguments = new Dictionary<string, IPartDefOrRef>();

                    var lCompositeRef = aPartToExpand.RuntimeType as CompositeTypeRef;

                    if ( lCompositeRef != null && mComposites.ContainsKey( lCompositeRef.Name ) )
                    {
                        var lOrigID = aPartToExpand.ID;
                        var lOrigName = aPartToExpand.Name;
                        var lOrigMetadata = aPartToExpand.Metadata;

                        // Setup feature arguments...
                        foreach ( var lFeature in aPartToExpand.Features )
                        {
                            lFeatureArguments[lFeature.Key] = lFeature.Value;
                        }

                        var lExpanded = CompositeExpander.expandPart( mComposites[lCompositeRef.Name].RootPart, lFeatureArguments, aPartToExpand.Properties, out var lAdditionalParts );
                        lExpanded.ID = lOrigID;
                        lExpanded.Name = lOrigName;

                        foreach ( var lMetadata in lOrigMetadata )
                        {
                            lExpanded.Metadata[lMetadata.Key] = lMetadata.Value;
                        }

                        // The expansion may have generated additional parts. If so, they needed to be
                        // added to the graph as well.
                        foreach ( var lAdditionalPart in lAdditionalParts )
                        {
                            addPart( lAdditionalPart, aTypeAliases );
                        }


#if DEBUG

                        lExpansionLog.WriteLine( "<!-- ========================================================================== -->" );
                        lExpansionLog.WriteLine( "<!-- ========================================================================== -->" );
                        lExpansionLog.WriteLine( "<!-- ========================================================================== -->" );
                        lExpansionLog.WriteLine( "<!-- ========================================================================== -->" );
                        lExpansionLog.WriteLine();

                        lExpansionLog.WriteLine( "<!-- ORIGINAL PART -->" );
                        lExpansionLog.WriteLine();

                        var lXML = XmlBlueprintWriter.writePart( aPartToExpand );
                        lExpansionLog.WriteLine( lXML.ToString() );
                        lExpansionLog.WriteLine();

                        lExpansionLog.WriteLine( "<!-- ADDITIONAL PARTS FROM EXPANSION -->" );
                        lExpansionLog.WriteLine();

                        foreach ( var lAdditionalPart in lAdditionalParts )
                        {
                            lXML = XmlBlueprintWriter.writePartDefOrRef( lAdditionalPart, true );
                            lExpansionLog.WriteLine( lXML.ToString() );
                            lExpansionLog.WriteLine();
                        }

                        lExpansionLog.WriteLine( "<!-- EXPANDED PART -->" );
                        lExpansionLog.WriteLine();

                        lXML = XmlBlueprintWriter.writePart( lExpanded );
                        lExpansionLog.WriteLine( lXML.ToString() );
                        lExpansionLog.WriteLine();

#endif

                        return lExpanded;
                    }

                    return aPartToExpand;
                }

                // Local method to handle the logic for adding a single part to the above lists
                // as well as to the overall Part Graph.
                void addPart( IPartDefOrRef aPart, IDictionary<string, TypeAlias> aTypeAliases )
                {
                    // This method only cares about actual part definitions, not
                    // references to existing parts. This is a mechanism to effectively
                    // flatten the hierarchy/nesting of Parts in the Blueprints.
                    if ( aPart is IPart lPartDef )
                    {
                        // check if it's composite and expand it if it is.
                        if ( lPartDef is Part lCompositeCandidate && lCompositeCandidate.RuntimeType is CompositeTypeRef )
                        {
                            aPart = lPartDef = expandCompositePart( lCompositeCandidate, aTypeAliases );
                        }

                        var lPartNode = new PartGraphNode( lPartDef );

                        lNodes[lPartNode.Name] = lPartNode;

                        if ( !string.IsNullOrWhiteSpace( lPartDef.Name ) )
                        {
                            lNamedNodes[lPartDef.Name] = lPartNode;
                        }

                        if ( aPart is UndefinedPart )
                        {
                            // Undefined parts can't be instantiated, so they are
                            // incomplete by default.
                            lPartNode.Complete = false;
                        }

                        if ( aPart is IHasLocationScheme lLocatorPart )
                        {
                            var lScheme = lLocatorPart.LocationScheme;
                            if ( !string.IsNullOrWhiteSpace( lScheme ) )
                            {
                                lLocatorNodes[lScheme] = lPartNode;
                            }
                        }

                        // null the Node's reference to a resolved Part Type.
                        // This is to protect the logic of the resolution
                        // process below in case the default for this property is
                        // ever non-null.
                        lPartNode.FullTypeDefinition = null;

                        if ( aPart is Part lFullPart )
                        {
                            // Recursively traverse through this part's features
                            // looking for additional part definitions.
                            foreach ( var lFeature in lFullPart.Features )
                            {
                                addPart( lFeature.Value, aTypeAliases );
                            }
                        }

                        if ( lPartDef is IHasRuntimeType lPartDefWithRuntimeType )
                        {
                            // Resolve the part's Runtime Type.
                            if ( lPartDefWithRuntimeType.RuntimeType is TypeAlias lTypeAlias
                                && aTypeAliases.ContainsKey( lTypeAlias.AliasName ) )
                            {
                                lPartNode.FullTypeDefinition = aTypeAliases[lTypeAlias.AliasName].Type;
                            }
                            else if ( lPartDefWithRuntimeType.RuntimeType is TypeDefinition lTypeDef )
                            {
                                lPartNode.FullTypeDefinition = lTypeDef;
                            }
                        }

                        if ( lPartNode.FullTypeDefinition == null && aPart is Part )
                        {
                            // Parts with no valid runtime type are incomplete.
                            lPartNode.Complete = false;
                        }


                        // Kind of a hack to ensure that only top-level part-lists/dictionaries are used
                        // for dependency graph calculations.
                        if ( aPart is Part || lPartNode.FullTypeDefinition != null )
                        {
                            mPartGraph.addNode( lPartNode );
                        }
                    }

                    if ( aPart is PartList lPartList )
                    {
                        // Recursively traverse the individual parts in a PartList
                        foreach ( var lPart in lPartList )
                        {
                            addPart( lPart, aTypeAliases );
                        }
                    }
                    else if ( aPart is PartDictionary lPartDict )
                    {
                        // Recursively traverse the individual parts in a PartDictionary.
                        foreach ( var lPart in lPartDict.Values )
                        {
                            addPart( lPart, aTypeAliases );
                        }
                    }
                } // end addPart local method

                // Traverse "base" parts. Recursively add sub-parts via the local method above.
                foreach ( var lBlueprint in mBlueprints )
                {
                    foreach ( var lPart in lBlueprint.Parts )
                    {
                        addPart( lPart.Value, lBlueprint.TypeAliases );
                    }
                }

                List<GraphEdge> lDependencyEdges = new List<GraphEdge>();

                // Method used to compute a dependency (edges) for a single Part "leaf".
                // Part "leaves" are generally references to other Parts.
                void buildEdgeForPartLeaf( PartGraphNode aParentNode, IPartDefOrRef aLeaf )
                {
                    GraphNode lDestination = null;
                    switch ( aLeaf )
                    {
                        case UriPartRef lRef:
                            if ( lLocatorNodes.ContainsKey( lRef.PartUri.Scheme ) )
                            {
                                lDestination = lLocatorNodes[lRef.PartUri.Scheme];

                            }
                            break;

                        case NamedPartRef lRef:
                            if ( lNamedNodes.ContainsKey( lRef.PartName ) )
                            {
                                lDestination = lNamedNodes[lRef.PartName];
                            }
                            break;

                        case IDPartRef lRef:
                            if ( lNodes.ContainsKey( lRef.PartID.ToString().ToLower() ) )
                            {
                                lDestination = lNodes[lRef.PartID.ToString()];
                            }
                            break;

                        case ConstantValue _:
                            // Constant values don't map to other parts, and shouldn't cause 
                            // node "completeness" to be false.
                            return;

                        case IPart lPart:
                            if ( lNodes.ContainsKey( lPart.ID.ToString().ToLower() ) )
                            {
                                lDestination = lNodes[lPart.ID.ToString().ToLower()];
                            }
                            else if ( lNodes.Where( aNode => aNode.Value.GraphedPart.Name == lPart.Name )
                                           .Select( aNode => aNode.Value )
                                           .FirstOrDefault() is PartGraphNode lNamedNode )
                            {
                                lDestination = lNamedNode;
                            }
                            break;
                    }

                    if ( lDestination != null )
                    {
                        var lNewEdge = new GraphEdge( Guid.NewGuid().ToString().ToLower(), aParentNode, lDestination, mDependsOnRelationship );
                        lDependencyEdges.Add( lNewEdge );
                    }
                    else
                    {
                        // Couldn't find a destination node, so the
                        // parent Part is incomplete.
                        aParentNode.Complete = false;
                    }
                }

                // Similar to the above method, but accounts for PartList/PartDictionary sub-trees.
                void buildEdgesForPart( PartGraphNode aParentNode, IPartDefOrRef aPart )
                {
                    switch ( aPart )
                    {
                        case PartList lPartList:
                            foreach ( var lSubPart in lPartList )
                            {
                                buildEdgesForPart( aParentNode, lSubPart );
                            }
                            break;

                        case PartDictionary lPartDict:
                            foreach ( var lSubPart in lPartDict.Values )
                            {
                                buildEdgesForPart( aParentNode, lSubPart );
                            }
                            break;

                        default:
                            buildEdgeForPartLeaf( aParentNode, aPart );
                            break;
                    }
                }

                // Traverse Parts and Features, generating Graph Edges
                // for each dependency.
                foreach ( var lNode in mPartGraph.NamedGraphNodes )
                {
                    switch ( lNode.Value.GraphedPart )
                    {
                        case Part lFullPart:
                            foreach ( var lFeature in lFullPart.Features )
                            {
                                buildEdgesForPart( lNode.Value, lFeature.Value );
                            }

                            // Handle properties that refer to a PartLocator uri.
                            foreach ( var lProp in lFullPart.Properties )
                            {
                                if ( lProp.Value is PropertyValue lPropValue && lPropValue.ValueUri != null )
                                {
                                    // Creating a fake UriRef to abuse our part leaf handlers above.
                                    var lUriRef = new UriPartRef()
                                    {
                                        PartUri = lPropValue.ValueUri
                                    };
                                    buildEdgeForPartLeaf( lNode.Value, lUriRef );
                                }
                            }
                            break;

                        case PartList lPartList:
                            foreach ( var lItem in lPartList )
                            {
                                buildEdgeForPartLeaf( lNode.Value, lItem );
                            }
                            break;

                        case PartDictionary lPartDictionary:
                            foreach ( var lItem in lPartDictionary.Values )
                            {
                                buildEdgeForPartLeaf( lNode.Value, lItem );
                            }
                            break;

                    }
                }

                // Actually add all of the edges to the graph.
                foreach ( var lNewEdge in lDependencyEdges )
                {
                    mPartGraph.addEdge( lNewEdge );
                }

                // Recursive function to compute dependency depth 
                // (0 = Has no dependencies, >0 = Has dependencies).
                void depthMarking( PartGraphNode aNode, ImmutableStack<PartGraphNode> aDepthStack )
                {
                    // Set the dependency depth of the node.
                    aNode.DependencyDepth = Math.Max( aNode.DependencyDepth, aDepthStack.count() - 1 );

                    // Get dependent edges (edges pointing to nodes that depend on this node).
                    var lDependents = aNode.getEdges<DependsOnRelationship>( GraphNode.SearchDirection.ToThisNode );

                    foreach ( var lDependent in lDependents )
                    {
                        PartGraphNode lSource = (PartGraphNode)lDependent.SourceNode;

                        // Follow edges only if not in the current dependency chain, and only if something 
                        // has a shallower to or the same depth as the current node. That means the dependent needs to be updated.
                        // Otherwise, it can be skipped.
                        if ( lSource.DependencyDepth <= aNode.DependencyDepth && !aDepthStack.containsItem( (PartGraphNode)lDependent.SourceNode ) )
                        {
                            // Recursively mark dependents with an increased depth. 
                            depthMarking( lDependent.SourceNode as PartGraphNode, aDepthStack.push( (PartGraphNode)lDependent.SourceNode ) );
                        }
                    }
                }

                // Query for Nodes that have no dependenciese (no outgoing edges).
                var lCompleteIndependentParts = mPartGraph.NamedGraphNodes.Where( aNode => !aNode.Value.HasDependencies && aNode.Value.Complete );

                // The nodes that have no dependencies act as the "start" points for the 
                // graph traversal, as they represent depth 0 for the dependency calculation.
                foreach ( var lRoot in lCompleteIndependentParts )
                {
                    depthMarking( lRoot.Value, new ImmutableStack<PartGraphNode>( lRoot.Value ) );
                }

                // "Loop Edges" here refers to dependent edges that have an invalid
                // depth relationship (valid edges have a source node depth that is
                // greater than destination node depth). These represent part of a
                // circular dependency.
                var lLoopEdges = mPartGraph.GraphEdges.Where( ( aEdge ) =>
                {
                    var lSource = aEdge.SourceNode as PartGraphNode;
                    var lDestination = aEdge.DestinationNode as PartGraphNode;

                    return lSource.DependencyDepth <= lDestination.DependencyDepth;
                } );

                // Invalidate the source nodes of loop edges. This breaks circular
                // dependencies and causes all nodes that are part of the circular sub-graph
                // to later be invalidated.
                foreach ( var lLoopEdge in lLoopEdges )
                {
                    ( (PartGraphNode)lLoopEdge.SourceNode ).Complete = false;
                }

                // Invalidate "incomplete" nodes.
                //     - Nodes that point to something that doesn't exist.
                //     - Nodes that point to something that is invalid.
                //     - Nodes that point to an undefined part.
                //     - Nodes that ARE a undefined part.
                //
                // These determinations have already been made. This local method
                // just recursively invalidates nodes that depend on invalid nodes.
                void invalidateNode( PartGraphNode aNode, Dictionary<string, PartGraphNode> aVisited = null )
                {
                    if ( aNode == null )
                    {
                        return;
                    }

                    var lVisitedNodes = aVisited;
                    if ( lVisitedNodes == null )
                    {
                        lVisitedNodes = new Dictionary<string, PartGraphNode>();
                    }

                    // Keep track of visitation. Prevent infinite loops.
                    lVisitedNodes[aNode.Name] = aNode;

                    aNode.Complete = false;

                    var lDependentEdges = aNode.getEdges<DependsOnRelationship>( GraphNode.SearchDirection.ToThisNode );

                    foreach ( var lOutEdge in lDependentEdges )
                    {
                        if ( !lVisitedNodes.ContainsKey( lOutEdge.SourceNode.Name ) )
                        {
                            invalidateNode( lOutEdge.SourceNode as PartGraphNode, lVisitedNodes );
                        }
                    }
                }

                // Find pre-marked incomplete nodes and recursively invalidate dependent nodes.
                var lIncompleteParts = mPartGraph.NamedGraphNodes.Where( aNode => !aNode.Value.Complete ).ToList();
                foreach ( var lIncompletePart in lIncompleteParts )
                {
                    invalidateNode( lIncompletePart.Value );
                }

#if DEBUG
            }
            finally
            {
                lExpansionLog.Dispose();
                XmlBlueprintWriter.OutputAllIDs = false;
            }
#endif
        }

        /// <summary>
        /// Call this method to assemble the parts in this container.
        /// Calling this assumes that there are no external parts required to
        /// load the blueprints.
        /// </summary>
        /// <exception cref="AggregateException">
        /// This method throws an AggregateException containing all
        /// downstream exceptions caught during the assembly process. If
        /// this exception is thrown, the entire build process failed.
        /// </exception>
        public void assembleParts()
        {
            assembleParts( new List<ExternalPartInstance>() );
        }

        /// <summary>
        /// Call this method to assemble the parts in this container.
        /// </summary>
        /// <param name="aExternalParts">
        /// This IEnumerable of <see cref="ExternalPartInstance"/> objects is used
        /// to initially register "external" parts with the container. If parts
        /// in the Blueprint List refer to external parts, this is how they will be found.
        /// This can be empty, but cannot be null.
        /// </param>
        /// <exception cref="AggregateException">
        /// This method throws an AggregateException containing all
        /// downstream exceptions caught during the assembly process. If
        /// this exception is thrown, the entire build process failed.
        /// </exception>
        public void assembleParts( IEnumerable<ExternalPartInstance> aExternalParts )
        {
            // Used internally to handle each ref type equally.
            object getPartByReference( IPartDefOrRef aPartReference )
            {
                switch ( aPartReference )
                {
                    case Part lPart when PartsByID.ContainsKey( lPart.ID ):
                        return PartsByID[lPart.ID];

                    case IDPartRef lPart when PartsByID.ContainsKey( lPart.PartID ):
                        return PartsByID[lPart.PartID];

                    case NamedPartRef lPart when PartsByName.ContainsKey( lPart.PartName ):
                        return PartsByName[lPart.PartName];

                    case UriPartRef lPart when PartLocators.ContainsKey( lPart.PartUri.Scheme ):
                        return PartLocators[lPart.PartUri.Scheme].getPartFromUri( lPart.PartUri );
                }

                return null;
            }

            // Local method used because it's only needed for the assemble parts operation.
            // Build the instance of a Part Dictionary
            object getPartDictionary( PartDictionary aPartDictionary, Type aDictionaryType )
            {
                int lItemCount = 0;

                try
                {
                    var lTypeAcceptable = false;
                    var lAddMethodAcceptable = false;

                    var lConcreteType = aDictionaryType;
                    var lElementType = default( Type );

                    // If the runtime type is an interface, only allow the interfaces that List<> implements.
                    // Then suggest that the runtime type to be instantiated should be List<>.
                    // This covers both the blueprint declaration, if the user doesn't care about specifying 
                    // the runtime type AND the part-list-in-a-feature scenario where the implementing Part
                    // explicitly doesn't want to declare a concrete type.
                    if ( aDictionaryType.IsInterface && aDictionaryType.IsGenericType && !aDictionaryType.IsGenericTypeDefinition )
                    {
                        var lGenericDef = aDictionaryType.GetGenericTypeDefinition();

                        // Check that the supplied interface is one of the expected interfaces, and work to extract the "value" type.
                        if ( lGenericDef == typeof( IDictionary<,> ) && aDictionaryType.GetGenericArguments()[0] == typeof( string ) )
                        {
                            // Much easier for the IDictionary<,> interface, as the second parameter is the "value" type.
                            lElementType = aDictionaryType.GetGenericArguments()[1];
                            lConcreteType = typeof( Dictionary<,> ).MakeGenericType( typeof( string ), lElementType );

                            lTypeAcceptable = true;
                        }
                        else if ( lGenericDef == typeof( ICollection<> ) ||
                                lGenericDef == typeof( IEnumerable<> )
                               )
                        {
                            // A bit harder for the IEnumerable<>/ICollection<> types, since the element type is required to
                            // be KeyValuePair<string,T>, where T is the "value" type.
                            var lDeclatedKeyValueType = aDictionaryType.GetGenericArguments()[0];
                            var lKeyValueTypeParams = lDeclatedKeyValueType.GetGenericArguments();
                            if ( lDeclatedKeyValueType.GetGenericTypeDefinition() == typeof( KeyValuePair<,> ) && lKeyValueTypeParams[0] == typeof( string ) )
                            {
                                lElementType = lKeyValueTypeParams[1];
                                lConcreteType = typeof( Dictionary<,> ).MakeGenericType( typeof( string ), lElementType );

                                lTypeAcceptable = true;
                            }
                        }
                    }
                    // This is not exhaustive. It's just meant to reduce exception routes to
                    // attempting to instantiate the concrete type.
                    else if ( !aDictionaryType.IsAbstract && !aDictionaryType.IsInterface )
                    {
                        // If the dictionary type was declared as a concrete type, discover the element type via ICollection<KeyValuePair<string,T>>.
                        var lInterfaces = aDictionaryType.GetInterfaces();
                        var lCollectionInterface = lInterfaces.FirstOrDefault( aType => aType.GetGenericTypeDefinition() == typeof( ICollection<> ) );

                        if ( lCollectionInterface != null )
                        {
                            // Identical to the code above, extracting the KeyValuePair, checking that it uses string keys and grabbing the "value" type.
                            var lDeclaredKeyValueType = lCollectionInterface.GetGenericArguments()[0];
                            var lKeyValueTypeParams = lDeclaredKeyValueType.GetGenericArguments();
                            if ( lDeclaredKeyValueType.GetGenericTypeDefinition() == typeof( KeyValuePair<,> ) && lKeyValueTypeParams[0] == typeof( string ) )
                            {
                                lTypeAcceptable = true;
                                lElementType = lKeyValueTypeParams[1];
                            }
                        }
                    }

                    var lAddMethod = default( MethodInfo );
                    var lKeyValueType = typeof( KeyValuePair<,> ).MakeGenericType( typeof( string ), lElementType );

                    // Find the method on the concrete type that implements ICollection<KeyValuePair<string,T>>.Add().
                    if ( lTypeAcceptable )
                    {

                        var lInterfaceToMap = typeof( ICollection<> ).MakeGenericType( lKeyValueType );
                        lAddMethod = lInterfaceToMap.GetMethod( "Add" );

                        var lMap = lConcreteType.GetInterfaceMap( lInterfaceToMap );

                        for ( var lIndex = 0; lIndex < lMap.InterfaceMethods.Length; lIndex++ )
                        {
                            if ( lMap.InterfaceMethods[lIndex] == lAddMethod )
                            {
                                lAddMethod = lMap.TargetMethods[lIndex];
                                lAddMethodAcceptable = true;
                                break;
                            }
                        }
                    }

                    if ( lAddMethodAcceptable )
                    {
                        // Instantiate and populate.
                        var lPartDictionaryInstance = Activator.CreateInstance( lConcreteType );
                        var lConverter = TypeDescriptor.GetConverter( lElementType );

                        foreach ( var lPartReference in aPartDictionary )
                        {
                            object lPartInstance = null;
                            if ( lPartReference.Value is ConstantValue lConstant )
                            {
                                if ( lElementType.IsAssignableFrom( typeof( string ) ) )
                                {
                                    lPartInstance = lConstant.Value;
                                }
                                else if ( lConverter != null && lConverter.CanConvertFrom( typeof( string ) ) )
                                {
                                    lPartInstance = lConverter.ConvertFromString( lConstant.Value );
                                }
                            }
                            else
                            {
                                lPartInstance = getPartByReference( lPartReference.Value );
                            }

                            if ( lPartInstance != null && lElementType.IsInstanceOfType( lPartInstance ) )
                            {
                                // Generate the KeyValuePair<string,T> object.
                                var lKeyValuePair = Activator.CreateInstance( lKeyValueType, lPartReference.Key, lPartInstance );

                                // Add it to the dictionary.
                                lAddMethod.Invoke( lPartDictionaryInstance, new[] { lKeyValuePair } );
                                lItemCount++;
                            }
                        }

                        if ( lItemCount == aPartDictionary.Count )
                        {
                            return lPartDictionaryInstance;
                        }
                    }
                }
                catch ( Exception lException )
                {
                    Logging.Logger.logException( LogLevel.Diagnostic, lException );
                }

                Logging.Logger.logError( LogLevel.Diagnostic, $"Unable to instantiate all parts of the PartDictionary. [{lItemCount}/{aPartDictionary.Count}] instantiated." );
                return null;
            }

            // Local method used because it's only needed for the assemble parts operation.
            // Build the instance of a Part List
            object getPartList( PartList aPartList, Type aListType )
            {
                // Note that this method is roughly the same as the getPartDictionary method
                // above. For rationale behind each step, read the comments over there.
                int lListCount = 0;

                try
                {
                    var lTypeAcceptable = false;
                    var lAddMethodAcceptable = false;

                    var lConcreteType = aListType;
                    var lElementType = default( Type );

                    // If the runtime type is an interface, only allow the interfaces that List<> implements.
                    // Then suggest that the runtime type to be instantiated should be List<>.
                    // This covers both the blueprint declaration, if the user doesn't care about specifying 
                    // the runtime type AND the part-list-in-a-feature scenario where the implementing Part
                    // explicitly doesn't want to declare a concrete type.
                    if ( aListType.IsInterface && aListType.IsGenericType && !aListType.IsGenericTypeDefinition )
                    {
                        var lGenericDef = aListType.GetGenericTypeDefinition();

                        if ( lGenericDef == typeof( IList<> ) ||
                           lGenericDef == typeof( ICollection<> ) ||
                           lGenericDef == typeof( IEnumerable<> )
                          )
                        {
                            lTypeAcceptable = true;
                            lElementType = aListType.GetGenericArguments()[0];
                            lConcreteType = typeof( List<> ).MakeGenericType( lElementType );
                        }
                    }
                    // This is not exhaustive. It's just meant to reduce exception routes to
                    // attempting to instantiate the concrete type.
                    else if ( !aListType.IsAbstract && !aListType.IsInterface )
                    {
                        // If the list type was declared as a concrete type, discover the element type via ICollection<>.
                        var lInterfaces = aListType.GetInterfaces();
                        var lCollectionInterface = lInterfaces.FirstOrDefault( aType => aType.GetGenericTypeDefinition() == typeof( ICollection<> ) );

                        if ( lCollectionInterface != null )
                        {
                            lTypeAcceptable = true;
                            lElementType = lCollectionInterface.GetGenericArguments()[0];
                        }
                    }

                    var lAddMethod = default( MethodInfo );

                    // Find the method on the concrete type that implements ICollection<>.Add().
                    if ( lTypeAcceptable )
                    {
                        var lInterfaceToMap = typeof( ICollection<> ).MakeGenericType( lElementType );
                        lAddMethod = lInterfaceToMap.GetMethod( "Add" );

                        var lMap = lConcreteType.GetInterfaceMap( lInterfaceToMap );

                        for ( var lIndex = 0; lIndex < lMap.InterfaceMethods.Length; lIndex++ )
                        {
                            if ( lMap.InterfaceMethods[lIndex] == lAddMethod )
                            {
                                lAddMethod = lMap.TargetMethods[lIndex];
                                lAddMethodAcceptable = true;
                                break;
                            }
                        }
                    }

                    if ( lAddMethodAcceptable )
                    {
                        // Instantiate and populate.
                        var lPartListInstance = Activator.CreateInstance( lConcreteType );
                        var lConverter = TypeDescriptor.GetConverter( lElementType );

                        foreach ( var lPartReference in aPartList )
                        {
                            object lPartInstance = null;

                            if ( lPartReference is ConstantValue lConstant )
                            {
                                if ( lElementType.IsAssignableFrom( typeof( string ) ) )
                                {
                                    lPartInstance = lConstant.Value;
                                }
                                else if ( lConverter != null && lConverter.CanConvertFrom( typeof( string ) ) )
                                {
                                    lPartInstance = lConverter.ConvertFromString( lConstant.Value );
                                }
                            }
                            else
                            {
                                lPartInstance = getPartByReference( lPartReference );
                            }

                            if ( lPartInstance != null && lElementType.IsInstanceOfType( lPartInstance ) )
                            {
                                lAddMethod.Invoke( lPartListInstance, new[] { lPartInstance } );
                                lListCount++;
                            }
                        }

                        if ( lListCount == aPartList.Count )
                        {
                            return lPartListInstance;
                        }
                    }
                }
                catch ( Exception lException )
                {
                    Logging.Logger.logException( LogLevel.Diagnostic, lException );
                }

                Logging.Logger.logError( LogLevel.Diagnostic, $"Unable to instantiate all parts of the PartList. [{lListCount}/{aPartList.Count}] instantiated." );
                return null;
            }

            if ( aExternalParts == null )
            {
                throw new ArgumentNullException( nameof( aExternalParts ) );
            }

            List<Exception> lExceptions = new List<Exception>();

            // Sanity check. If somehow there is no graph, attempt to build it.
            if ( mPartGraph == null )
            {
                buildGraph();

                // If there's STILL no graph. That's the end of the line.
                if ( mPartGraph == null )
                {
                    throw new InvalidOperationException( "The part graph could not be built." );
                }
            }

            // Register provided external parts.
            foreach ( var lExternalInstance in aExternalParts )
            {
                PartsByID[lExternalInstance.ID] = lExternalInstance.PartInstance;

                if ( !string.IsNullOrWhiteSpace( lExternalInstance.Name ) )
                {
                    PartsByName[lExternalInstance.Name] = lExternalInstance.PartInstance;
                }

                if ( !string.IsNullOrWhiteSpace( lExternalInstance.LocationScheme )
                    && lExternalInstance.PartInstance is IPartLocator lExternalLocator )
                {
                    PartLocators[lExternalInstance.LocationScheme] = lExternalLocator;
                }
            }

            // Check that the user supplied all external parts declared in the blueprints.
            var lDeclaredExternalParts = mPartGraph.NamedGraphNodes.Select( aNode => aNode.Value.GraphedPart ).OfType<ExternalPart>();

            foreach ( var lDeclaredExternalPart in lDeclaredExternalParts )
            {
                // If the external declaration in the blueprint has ID, that ID must also exist in the
                // provided external parts. Same for Name/Scheme.
                bool lExternIsGood = ( lDeclaredExternalPart.ID == Guid.Empty || PartsByID.ContainsKey( lDeclaredExternalPart.ID ) )
                                     && ( string.IsNullOrWhiteSpace( lDeclaredExternalPart.Name ) || PartsByName.ContainsKey( lDeclaredExternalPart.Name ) )
                                     && ( string.IsNullOrWhiteSpace( lDeclaredExternalPart.LocationScheme ) || PartLocators.ContainsKey( lDeclaredExternalPart.LocationScheme ) );

                if ( !lExternIsGood )
                {
                    if ( GlobalProperties.FileLocationInfo.hasValue( lDeclaredExternalPart ) )
                    {
                        var lLocation = GlobalProperties.FileLocationInfo.getValue( lDeclaredExternalPart );
                        lExceptions.Add( new MissingExternalPartException( lDeclaredExternalPart, lLocation ) );
                    }
                    else
                    {
                        lExceptions.Add( new MissingExternalPartException( lDeclaredExternalPart ) );
                    }
                }
            }

            if ( lExceptions.Count > 0 )
            {
                // One or more external parts were missing. Throw a final aggregate exception.
                throw new AggregateException( "Not all required external part instances were provided by the caller. See InnerExceptions for more information.", lExceptions );
            }

            var lPartSpecsByName = mSpecifications.ToDictionary( aSpec => aSpec.PartType.FullName );

            // Get a collection of all the parts that can be built from the graph, and do so
            // in dependency order (least dependent on other parts first!).
            var lGoodParts = mPartGraph.NamedGraphNodes.Where( aNode => aNode.Value.Complete ).OrderBy( aNode => aNode.Value.DependencyDepth );

            // Save a list of incomplete parts so that Fabrica users can debug additional reasons their
            // blueprint didn't fully assemble.
            this.IncompleteParts = mPartGraph.NamedGraphNodes.Where( aNode => !aNode.Value.Complete )
                                                             .OrderByDescending( aNode => aNode.Value.DependencyDepth )
                                                             .ToDictionary( aKey => aKey.Value.GraphedPart.ID, aValue => aValue.Value.GraphedPart );

            var lLogTimes = LogPartInstantiationTimes;
            StreamWriter lTimeLog = null;

            try
            {
                if ( lLogTimes )
                {
                    try
                    {
                        if ( File.Exists( PartInstantiationTimesLogPath ) )
                        {
                            File.Delete( PartInstantiationTimesLogPath );
                        }
                        lTimeLog = new StreamWriter( PartInstantiationTimesLogPath );
                    }
                    catch( Exception )
                    {
                        Logging.Logger.logError( $"Could not open/create Part Instantiation Time log '{PartInstantiationTimesLogPath}'." );
                        lLogTimes = false;
                    }
                }

                // Build Parts. This will go in dependency order. Those parts
                // that do not depend on other parts go first, then parts that depend on those
                // go next, etc. The order is designed so that all dependencies for a part
                // are loaded before that part is instantiated.
                foreach ( var lPart in lGoodParts )
                {
                    try
                    {
                        object lPartInstance = null;

                        // Sanity check, in case a non-Part got in here somehow...
                        if ( lPart.Value.GraphedPart is Part lFullPart )
                        {
                            var lPartRuntimeTypeDef = lPart.Value.FullTypeDefinition;
                            Type lPartRuntimeType = TypeDefinition.TypeFromDefinition( lPartRuntimeTypeDef );

                            // Make sure the part has a defined runtime type, and associated Part Specification.
                            if ( lPartRuntimeType != null && lPartSpecsByName.ContainsKey( lPartRuntimeTypeDef.FullName ) )
                            {
                                var lTargetSpec = lPartSpecsByName[lPartRuntimeTypeDef.FullName];

                                // Part Specifications of generic part classes will need to be
                                // further defined in order to be used.
                                if ( lTargetSpec.PartType.IsGenericTypeDefinition )
                                {
                                    // Get the runtime type's type arguments.
                                    var lRuntimeTypeArgs = lPartRuntimeType.GetGenericArguments();

                                    // Build a new part specification from the fully defined part spec generic class.
                                    Type lInflatedSpecType = lTargetSpec.PartType.MakeGenericType( lRuntimeTypeArgs );
                                    lTargetSpec = PartSpecification.createPartSpecification( lInflatedSpecType );
                                }

                                IPartConstructorInfo lConstructorInfo = null;

                                if ( !string.IsNullOrWhiteSpace( lFullPart.Constructor ) )
                                {
                                    // Requesting a named constructor
                                    if ( lTargetSpec.NamedPartConstructors.ContainsKey( lFullPart.Constructor ) )
                                    {
                                        lConstructorInfo = lTargetSpec.NamedPartConstructors[lFullPart.Constructor];
                                    }
                                }
                                else
                                {
                                    // Requesting default constructor
                                    lConstructorInfo = lTargetSpec.DefaultConstructor;
                                }

                                // Die here if we can't find a constructor.
                                if ( lConstructorInfo == null )
                                {
                                    throw new Exception( $"Could not get part constructor '{lFullPart.Constructor}'." );
                                }

                                // Build Features Collection. This will be used to actually instantiate the part.
                                Dictionary<string, object> lFeatures = new Dictionary<string, object>();

                                // The Part Specification defines what Features are available/required,
                                // so we iterate using that as a reference, since the configured part may
                                // not be complete.
                                foreach ( var lFeature in lConstructorInfo.Features )
                                {
                                    // Check if the configured part definition has something for
                                    // the specification's feature...
                                    if ( lFullPart.Features.ContainsKey( lFeature.Key ) )
                                    {
                                        var lFeatureRef = lFullPart.Features[lFeature.Key];
                                        object lFeatureValue = null;

                                        switch ( lFeatureRef )
                                        {
                                            // For PartLists and PartDictionaries that weren't declared at
                                            // the "top level", they need to be handled here, using the Feature's
                                            // declared Type to instantiate the list/dictionary.
                                            case PartList lRef:
                                                lFeatureValue = getPartList( lRef, lFeature.Value );
                                                break;

                                            case PartDictionary lRef:
                                                lFeatureValue = getPartDictionary( lRef, lFeature.Value );
                                                break;

                                            case ConstantValue lConstant:

                                                // PartSpecification's "instantiatePart" will attempt to auto-convert from strings
                                                lFeatureValue = lConstant.Value;
                                                break;

                                            default:
                                                lFeatureValue = getPartByReference( lFeatureRef );
                                                break;
                                        }

                                        // Make sure a feature's part was successfully generated/received.
                                        if ( lFeatureValue != null )
                                        {
                                            lFeatures[lFeature.Key] = lFeatureValue;
                                        }
                                    }
                                }

                                // Build Features Collection. This will be used to actually instantiate the part.
                                Dictionary<string, object> lProperties = new Dictionary<string, object>();

                                // The Part Specification defines what Features are available/required,
                                // so we iterate using that as a reference, since the configured part may
                                // not be complete.
                                foreach ( var lProperty in lTargetSpec.Properties )
                                {
                                    // Check if the configured part definition has something for
                                    // the specification's feature...
                                    if ( lFullPart.Properties.ContainsKey( lProperty.Key ) )
                                    {
                                        IPropertyValueOrSlot lPotentialProp = lFullPart.Properties[lProperty.Key];

                                        if ( lPotentialProp is PropertyValue lPropValue )
                                        {
                                            if ( lPropValue.Value != null )
                                            {
                                                lProperties[lProperty.Key] = lPropValue.Value;
                                            }
                                            else
                                            {
                                                var lPropScheme = lPropValue.ValueUri.Scheme;
                                                if ( PartLocators.ContainsKey( lPropScheme ) )
                                                {
                                                    object lPropertyObject = PartLocators[lPropScheme].getPartFromUri( lPropValue.ValueUri );

                                                    if ( lPropertyObject != null )
                                                    {
                                                        lProperties[lProperty.Key] = lPropertyObject;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if ( lLogTimes )
                                {
                                    using ( var lPIT = new PartInstantiationTimer( lFullPart, lTimeLog ) )
                                    {
                                        // This will throw an exception if it doesn't work.
                                        lPartInstance = lTargetSpec.instantiatePart( lFullPart.Constructor, lFeatures, lProperties );
                                    }
                                }
                                else
                                {
                                    // This will throw an exception if it doesn't work.
                                    lPartInstance = lTargetSpec.instantiatePart( lFullPart.Constructor, lFeatures, lProperties );
                                }

                                //Checking if metadata has a non empty dictionary, if so we need to attach it to the object.
                                if ( lFullPart.Metadata.Any() )
                                {
                                    //Metadata has elements, we need to attach the dictionary to the instance
                                    GlobalProperties.PartMetadata.setValue( lPartInstance, lFullPart.Metadata );
                                }

                                // For By Uri-Scheme (Part Locators only!)
                                if ( !string.IsNullOrWhiteSpace( lFullPart.LocationScheme )
                                    && lTargetSpec.IsPartLocator
                                    && lPartInstance is IPartLocator lLocator
                                    && lFullPart.LocationScheme == lTargetSpec.PartLocationScheme )
                                {
                                    PartLocators[lFullPart.LocationScheme] = lLocator;
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException( $"Attempt to get type '{lPartRuntimeTypeDef}' from type definition failed. Check that the type exists." );
                            }
                        }
                        else if ( lPart.Value.GraphedPart is PartList lPartList )
                        {
                            var lListType = TypeDefinition.TypeFromDefinition( lPart.Value.FullTypeDefinition );

                            if ( lListType == null )
                            {
                                throw new InvalidOperationException( $"Attempt to get type '{lPart.Value.FullTypeDefinition}' from type definition failed. Check that the type exists." );
                            }

                            lPartInstance = getPartList( lPartList, lListType );
                        }
                        else if ( lPart.Value.GraphedPart is PartDictionary lPartDictionary )
                        {
                            var lDictionaryType = TypeDefinition.TypeFromDefinition( lPart.Value.FullTypeDefinition );

                            if ( lDictionaryType == null )
                            {
                                throw new InvalidOperationException( $"Attempt to get type '{lPart.Value.FullTypeDefinition}' from type definition failed. Check that the type exists." );
                            }

                            lPartInstance = getPartDictionary( lPartDictionary, lDictionaryType );
                        }

                        if ( lPartInstance != null )
                        {
                            // Success! Now we add the results to our various lookup collections.
                            if ( lPart.Value.GraphedPart.ID != Guid.Empty )
                            {
                                PartsByID[lPart.Value.GraphedPart.ID] = lPartInstance;
                            }

                            // For By-Name...
                            if ( !string.IsNullOrWhiteSpace( lPart.Value.GraphedPart.Name ) )
                            {
                                PartsByName[lPart.Value.GraphedPart.Name] = lPartInstance;
                            }
                        }
                    }
                    catch ( Exception lException )
                    {
                        // Catch the exceptions and rethrow with context. Keep track of it.
                        var lPartIdentifier = getPartNameOrID( lPart.Value.GraphedPart );

                        if ( GlobalProperties.FileLocationInfo.hasValue( lPart.Value.GraphedPart ) )
                        {
                            var lLocation = GlobalProperties.FileLocationInfo.getValue( lPart.Value.GraphedPart );
                            lExceptions.Add( new FailedPartAssemblyException( lPartIdentifier, lLocation, lException ) );
                        }
                        else
                        {
                            lExceptions.Add( new FailedPartAssemblyException( lPartIdentifier, lException ) );
                        }
                    }
                }
            }
            finally
            {
                if ( lTimeLog != null )
                {
                    lTimeLog.Dispose();
                }
            }

            if ( lExceptions.Count > 0 )
            {
                // One or more parts failed to build. Throw a final aggregate exception.
                throw new AggregateException( "Could not assemble parts in part graph. See InnerExceptions for more information.", lExceptions );
            }
        }

        /// <summary>
        /// If set to true, Fabrica will log the amount of time it took to instantiate each
        /// part in the part graph during the assemble parts operation. The path for this log file can
        /// be controlled using the <see cref="PartInstantiationTimesLogPath"/> property.
        /// </summary>
        public bool LogPartInstantiationTimes { get; set; } = false;

        /// <summary>
        /// The path to a file that Fabrica should log part instantiation times to if <see cref="LogPartInstantiationTimes"/>
        /// is set to true. By default, this is set to "PartInstantiationTimes.csv". The path specified here
        /// will be overwritten.
        /// </summary>
        public string PartInstantiationTimesLogPath { get; set; } = "PartInstantiationTimes.csv";

        /// <summary>
        /// Retrieves the name or, if none exists, the ID of the specified Part.
        /// </summary>
        /// <param name="aPart">
        /// The part to extract an identifier for.
        /// </param>
        /// <returns>
        /// The part name or, if none exists, ID.
        /// </returns>
        private static string getPartNameOrID( IPart aPart )
        {
            var lID = aPart.ID.ToString().ToUpper();
            var lName = aPart.Name;
            return !string.IsNullOrWhiteSpace( lName ) ? lName : lID;
        }

        /// <summary>
        /// This is a collection of Parts stored by their ID. This collection is empty until
        /// <see cref="PartContainer.assembleParts()"/> is called, is successful, and results in usable parts.
        /// </summary>
        public IDictionary<Guid, object> PartsByID { get; private set; } = new Dictionary<Guid, object>();

        /// <summary>
        /// This is a collection of incomplete Parts stored by their ID. Parts are considered incomplete if any
        /// of the parts it relies on are non-existent or incomplete. This means that any part with an incorrect
        /// part reference may cause a cascade of incomplete parts throughout the blueprint. This collection is 
        /// empty until <see cref="PartContainer.assembleParts()"/> is called. 
        /// </summary>
        public IDictionary<Guid, IPart> IncompleteParts { get; private set; } = new Dictionary<Guid, IPart>();

        /// <summary>
        /// <para>
        /// This is a collection of Parts stored by their Name. This collection is empty until
        /// <see cref="PartContainer.assembleParts()"/> is called, is successful, and results in usable parts
        /// that have defined names. 
        /// </para>
        /// <para>
        /// Note, parts are not required to have names. Unnamed parts can only be retrieved via
        /// the part ID.
        /// </para>
        /// </summary>
        public IDictionary<string, object> PartsByName { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// <para>
        /// This is a collection of Part Locators stored by their Supporting Uri Scheme. This collection is empty until
        /// <see cref="PartContainer.assembleParts()"/> is called, is successful, and results in usable part locators.
        /// </para>
        /// </summary>
        public IDictionary<string, IPartLocator> PartLocators { get; private set; } = new Dictionary<string, IPartLocator>();

        /// <summary>
        /// <see cref="PartContainer"/> implements <see cref="IDisposable"/> in order to Dispose
        /// any loaded Parts that are <see cref="IDisposable"/>. This class does not itself manage
        /// any Unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach ( var lPart in PartsByID )
            {
                if ( lPart.Value is IDisposable lDisposablePart )
                {
                    lDisposablePart.Dispose();
                }
            }
        }
    }
}
