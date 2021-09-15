// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace GEAviation.Fabrica.Structures.GeneralGraph
{
    public class GraphNode
    {
        public enum SearchDirection
        {
            ToThisNode,
            FromThisNode,
            Undirected
        }

        public HashSet<GraphEdge> Edges { get; set; }

        /// <summary>
        /// Gets/sets the unique name of this node. The name is presumed to be unique throughout the parent Graph.
        /// </summary>
        public virtual string Name { get; protected set; }

        public IEnumerable<GraphEdge> AllEdges
        {
            get
            {
                return this.Edges;
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public GraphNode()
        {
            this.Edges = new HashSet<GraphEdge>();
            this.Name = String.Empty;
        }

        public GraphEdge getEdge<TRelationshipType>( SearchDirection aDirection )
        {
            IEnumerable<GraphEdge> lEdges = getEdges<TRelationshipType>( aDirection );
            GraphEdge lReturn = null;
            foreach( GraphEdge iEdge in lEdges )
            {
                lReturn = iEdge;
                break;
            }
            return lReturn;
        }

        public GraphEdge getEdge( SearchDirection aDirection )
        {
            IEnumerable<GraphEdge> lEdges = getEdges( aDirection );
            GraphEdge lReturn = null;
            foreach( GraphEdge iEdge in lEdges )
            {
                lReturn = iEdge;
                break;
            }
            return lReturn;
        }

        public IEnumerable<GraphEdge> getEdges( Func<GraphEdge, bool> aSearchCondition )
        {
            if( aSearchCondition == null )
            {
                throw new ArgumentNullException( "aSearchCondition" );
            }

            foreach( GraphEdge lEdge in this.Edges )
            {
                if( aSearchCondition( lEdge ) )
                {
                    yield return lEdge;
                }
            }
        }

        public IEnumerable<GraphEdge> getEdges( SearchDirection aDirection )
        {
            return this.getEdges( delegate( GraphEdge aEdge )
            {
                switch( aDirection )
                {
                    case SearchDirection.FromThisNode:
                        if( aEdge.SourceNode == this )
                        {
                            return true;
                        }
                        break;

                    case SearchDirection.ToThisNode:
                        if( aEdge.DestinationNode == this )
                        {
                            return true;
                        }
                        break;

                    default:
                        return true;
                }

                return false;
            } );
        }

        public IEnumerable<GraphEdge> getEdges<TRelationshipType>( SearchDirection aDirection )
        {
            return this.getEdges( delegate( GraphEdge aEdge )
            {
                if( aEdge.Relationship is TRelationshipType )
                {
                    switch( aDirection )
                    {
                        case SearchDirection.FromThisNode:
                            if( aEdge.SourceNode == this )
                            {
                                return true;
                            }
                            break;

                        case SearchDirection.ToThisNode:
                            if( aEdge.DestinationNode == this )
                            {
                                return true;
                            }
                            break;

                        default:
                            return true;
                    }
                }

                return false;
            } );
        }

        public GraphEdge linkFrom<TRelType>( GraphNode aSource ) where TRelType : RelationshipType
        {
            TRelType lRelationship = Activator.CreateInstance<TRelType>();
            return new GraphEdge( aSource, this, lRelationship );
        }

        public GraphEdge linkTo<TRelType>( GraphNode aDestination ) where TRelType : RelationshipType
        {
            TRelType lRelationship = Activator.CreateInstance<TRelType>();
            return new GraphEdge( this, aDestination, lRelationship );
        }
    }
}
