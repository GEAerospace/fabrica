// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace GEAviation.Fabrica.Structures.GeneralGraph
{
    /// <summary>
    /// Represents an edge between two GraphNode objects. Expresses direction of relationshipTag by referring to SourceNode and DestinationNode. Can be
    /// undirected( == true).
    /// </summary>
    [DebuggerDisplay( "{ToRelationshipString()}" )]
    public class GraphEdge
    {
        /// <summary>
        /// The GraphNode at which this edge ends. Ex: SourceNode INHERITS_FROM DestinationNode
        /// </summary>
        public GraphNode DestinationNode { get; protected set; }

        /// <summary>
        /// Gets/sets the unique name of this node. The name is presumed to be unique throughout the parent Graph.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Describes the relationship between the SourceNode and DestinationNode.
        /// </summary>
        public RelationshipType Relationship { get; protected set; }

        /// <summary>
        /// The GraphNode at which this edge originates. Ex: SourceNode INHERITS_FROM DestinationNode
        /// </summary>
        public GraphNode SourceNode { get; protected set; }

        /// <summary>
        /// True if the edge has no direction, false if it is a directed relationship.
        /// </summary>
        public bool UndirectedEdge { get; protected set; }

        /// <summary>
        /// Builds a GraphEdge with the input elements
        /// </summary>
        /// <param name="aName">
        /// The name of this edge, presumed to be unique throughout the containing graph.
        /// </param>
        /// <param name="aSource">
        /// The source this edge comes from.
        /// </param>
        /// <param name="aDestination">
        /// The destination this edge points to.
        /// </param>
        /// <param name="aRelationship">
        /// The type of the relationship between source and destination nodes.
        /// </param>
        /// <param name="aUndirected">
        /// True if the edge has no direction, false if it is a directed relationship.
        /// </param>
        public GraphEdge( string aName, GraphNode aSource, GraphNode aDestination, RelationshipType aRelationship, bool aUndirected )
        {
            this.SourceNode = aSource;
            this.DestinationNode = aDestination;
            this.Relationship = aRelationship;
            this.UndirectedEdge = aUndirected;
            this.Name = aName;
            if( string.IsNullOrWhiteSpace( aName ) )
            {
                this.Name = this.ToString();
            }
        }

        /// <summary>
        /// Builds a GraphEdge with the input elements. The GraphEdge will be undirected.
        /// </summary>
        /// <param name="aName">
        /// The name of this edge, presumed to be unique throughout the containing graph.
        /// </param>
        /// <param name="aSource">
        /// The source this edge comes from.
        /// </param>
        /// <param name="aDestination">
        /// The destination this edge points to.
        /// </param>
        /// <param name="aRelationship">
        /// The type of the relationship between source and destination nodes.
        /// </param>
        public GraphEdge( string aName, GraphNode aSource, GraphNode aDestination, RelationshipType aRelationship )
            : this( aName, aSource, aDestination, aRelationship, false ) { }

        /// <summary>
        /// Builds a GraphEdge with the input elements. The GraphEdge will contain a default name.
        /// </summary>
        /// <param name="aSource">
        /// The source this edge comes from.
        /// </param>
        /// <param name="aDestination">
        /// The destination this edge points to.
        /// </param>
        /// <param name="aRelationship">
        /// The type of the relationship between source and destination nodes.
        /// </param>
        /// <param name="aUndirected">
        /// True if the edge has no direction, false if it is a directed relationship.
        /// </param>
        public GraphEdge( GraphNode aSource, GraphNode aDestination, RelationshipType aRelationship, bool aUndirected )
            : this( "", aSource, aDestination, aRelationship, aUndirected ) { }

        /// <summary>
        /// Builds a GraphEdge with the input elements. The GraphEdge will be undirected and contain a default name.
        /// </summary>
        /// <param name="aSource">
        /// The source this edge comes from.
        /// </param>
        /// <param name="aDestination">
        /// The destination this edge points to.
        /// </param>
        /// <param name="aRelationship">
        /// The type of the relationship between source and destination nodes.
        /// </param>
        public GraphEdge( GraphNode aSource, GraphNode aDestination, RelationshipType aRelationship )
            : this( "", aSource, aDestination, aRelationship, false ) { }

        /// <summary>
        /// Helper function to help cast the DestinationNode stored in this GraphEdge to the underlying GraphNode type.
        /// </summary>
        /// <typeparam name="GraphNodeType">
        /// The type of GraphNode that is expected.
        /// </typeparam>
        /// <returns>
        /// The stored instance of GraphNode, null if there is no stored GraphNode or null if the stored GraphNode is not the type expected.
        /// </returns>
        public GraphNodeType getSpecificDestinationNode<GraphNodeType>() where GraphNodeType : GraphNode
        {
            return this.DestinationNode as GraphNodeType;
        }

        /// <summary>
        /// Helper function to help cast the Relationship stored in this GraphEdge to the underlying type.
        /// </summary>
        /// <typeparam name="TRelationshipType">
        /// The type of RelationshipType that is expected.
        /// </typeparam>
        /// <returns>
        /// The stored instance of RelationshipType, null if there is no stored relationship or null if the stored relationship is not the type expected.
        /// </returns>
        public TRelationshipType getSpecificRelationship<TRelationshipType>() where TRelationshipType : RelationshipType
        {
            return this.Relationship as TRelationshipType;
        }

        /// <summary>
        /// Helper function to help cast the SourceNode stored in this GraphEdge to the underlying GraphNode type.
        /// </summary>
        /// <typeparam name="GraphNodeType">
        /// The type of GraphNode that is expected.
        /// </typeparam>
        /// <returns>
        /// The stored instance of GraphNode, null if there is no stored GraphNode or null if the stored GraphNode is not the type expected.
        /// </returns>
        public GraphNodeType getSpecificSourceNode<GraphNodeType>() where GraphNodeType : GraphNode
        {
            return this.SourceNode as GraphNodeType;
        }

        /// <summary>
        /// Returns a string that provides a human readable representation of this GraphEdge and the contained relationship.
        /// </summary>
        /// <returns>
        /// The human-readable relationship string.
        /// </returns>
        public virtual string ToRelationshipString()
        {
            if( this.Relationship != null )
            {
                return string.Format( this.Relationship.getRelationshipString(), this.SourceNode.ToString(), this.DestinationNode.ToString() );
            }
            else
            {
                return this.ToString();
            }
        }

        // No document comments as this is an overridden base class member.
        public override string ToString()
        {
            return String.Format( "{0}{1}{2}", this.SourceNode.ToString(), this.UndirectedEdge ? "<->" : "-->", this.DestinationNode.ToString() );
        }
    }
}
