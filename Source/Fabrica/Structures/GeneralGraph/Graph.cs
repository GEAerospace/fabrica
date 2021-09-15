// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace GEAviation.Fabrica.Structures.GeneralGraph
{
    public class Graph<NodeType, EdgeType> where NodeType : GraphNode where EdgeType : GraphEdge
    {
        public HashSet<EdgeType> GraphEdges { get; protected set; }
        public HashSet<NodeType> GraphNodes { get; protected set; }
        public Dictionary<string, EdgeType> NamedGraphEdges { get; protected set; }

        public Dictionary<string, NodeType> NamedGraphNodes { get; protected set; }

        /// <summary>
        /// Default constructor. Initializes with empty list of GraphNodes
        /// </summary>
        public Graph()
            : this( null ) { }

        /// <summary>
        /// Constructor for Graph that takes in GraphNodes to store.
        /// </summary>
        /// <param name="aGraphNodes">
        /// The GraphNodes to store. Can be null and filled with AddNode later.
        /// </param>
        public Graph( HashSet<NodeType> aGraphNodes )
        {
            this.GraphNodes = new HashSet<NodeType>();
            this.GraphEdges = new HashSet<EdgeType>();
            this.NamedGraphNodes = new Dictionary<string, NodeType>();
            this.NamedGraphEdges = new Dictionary<string, EdgeType>();

            if( aGraphNodes != null )
            {
                foreach( NodeType lNode in aGraphNodes )
                {
                    this.addNode( lNode );
                }
            }
        }

        /// <summary>
        /// Adds an edge to the graph, including additions to the internal dictionary and list. Then adds the edge to the source and destination nodes.
        /// </summary>
        /// <param name="aEdge">
        /// The Edge to add to the Graph.
        /// </param>
        /// <returns>
        /// </returns>
        public virtual bool addEdge( EdgeType aEdge )
        {
            if( this.GraphEdges.Add( aEdge ) )
            {
                aEdge.SourceNode.Edges.Add( aEdge );
                aEdge.DestinationNode.Edges.Add( aEdge );

                this.addNode( (NodeType)aEdge.SourceNode );
                this.addNode( (NodeType)aEdge.DestinationNode );

                if( EdgeAddedEvent != null )
                {
                    EdgeAddedEvent.Invoke( this, aEdge );
                }

                return true;
            }

            return false;
        }

        public virtual bool addNode( NodeType aNode )
        {
            if( aNode == null )
            {
                throw new ArgumentNullException( "aNode" );
            }

            if( this.GraphNodes.Add( aNode ) )
            {
                if( this.NamedGraphNodes.ContainsKey( aNode.Name ) )
                {
                    if( DuplicateNodeNameEvent != null )
                    {
                        DuplicateNodeNameEvent.Invoke( this, aNode, this.NamedGraphNodes[aNode.Name] );
                    }
                }
                else
                {
                    this.NamedGraphNodes[aNode.Name] = aNode;
                }

                foreach( GraphEdge lEdge in aNode.AllEdges )
                {
                    this.addEdge( (EdgeType)lEdge );
                }

                return true;
            }

            return false;
        }

        public event Action<object, NodeType, NodeType> DuplicateNodeNameEvent;

        public event Action<object, GraphEdge> EdgeAddedEvent;
    }
}
