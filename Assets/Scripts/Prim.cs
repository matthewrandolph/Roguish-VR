using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Graphs;

public static class Prim
{
    public class Edge : Graphs.Edge
    {
        public float Distance { get; private set; }

        public Edge(Vertex u, Vertex v) : base(u, v)
        {
            Distance = Vector3.Distance(u.Position, v.Position);
        }

        public static bool operator ==(Edge left, Edge right)
        {
            return (left.U == right.U && left.V == right.V)
                   || (left.U == right.V && left.V == right.U);
        }

        public static bool operator !=(Edge left, Edge right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge e)
            {
                return this == e;
            }

            return false;
        }

        public bool Equals(Edge e)
        {
            return this == e;
        }

        public override int GetHashCode()
        {
            return U.GetHashCode() ^ V.GetHashCode();
        }
    }

    // creates a List of Edges representing the members of a Minimum spanning tree
    // using the distance between Vertexes as edge weights
    public static List<Edge> MinimumSpanningTree(List<Edge> edges, Vertex start)
    {
        var openSet = new HashSet<Vertex>();
        var closedSet = new HashSet<Vertex>();

        // add all Vertexes to the open set
        foreach (var edge in edges)
        {
            openSet.Add(edge.U);
            openSet.Add(edge.V);
        }

        // add the starting Vertex to the closed set
        closedSet.Add(start);

        // the list of Edges representing the MST
        var results = new List<Edge>();

        // repeat until all Vertexes have been closed
        while (openSet.Count > 0)
        {
            // indicates the current Edge has the smallest cost
            var chosen = false;
            
            // the Edge that has the smallest cost so far
            Edge chosenEdge = null;
            
            // the cost of the Edge that has the smallest cost so far
            var minWeight = float.PositiveInfinity;

            foreach (var edge in edges)
            {
                // check if the Edge is connected to the closed set, but not inside the closed set
                var closedVertices = 0;
                if (!closedSet.Contains(edge.U)) closedVertices++;
                if (!closedSet.Contains(edge.V)) closedVertices++;
                if (closedVertices != 1) continue;

                // if this Edge is closer than the chosen Edge, replace it as the chosen Edge
                if (edge.Distance < minWeight)
                {
                    chosenEdge = edge;
                    chosen = true;
                    minWeight = edge.Distance;
                }
            }

            // if no Edges have been chosen, the MST won't change anymore, so break out of the while loop
            if (!chosen) break;
            
            results.Add(chosenEdge);
            openSet.Remove(chosenEdge.U);
            openSet.Remove(chosenEdge.V);
            closedSet.Add(chosenEdge.U);
            closedSet.Add(chosenEdge.V);
        }

        return results;
    }
}

