using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;

public class Generator2D : MonoBehaviour
{
    enum CellType
    {
        None,
        Room,
        Hallway
    }

    class Room
    {
        // the boundary of our Room in worldspace
        public RectInt bounds;

        public Room(Vector2Int location, Vector2Int size)
        {
            bounds = new RectInt(location, size);
        }

        // checks if two rooms overlap
        public static bool Intersect(Room a, Room b)
        {
            return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) 
                     || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x) 
                     || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) 
                     || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y));
        }
    }

    // the size of the area the rooms can be generated in
    [SerializeField] private Vector2Int size;

    // number of tries to generate rooms
    [SerializeField] private int roomCount;
    
    // maximum size a generated room can be
    [SerializeField] private Vector2Int roomMaxSize;
    
    // fields for testing the generation system
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private Material redMaterial;
    [SerializeField] private Material blueMaterial;

    // reference to the random number generator
    private Random random;
    private Grid2D<CellType> grid;
    
    // a list of rooms that have been added to worldspace
    private List<Room> rooms;

    // a Delaunay graph containing all the rooms
    private Delaunay2D delaunay;

    //
    private HashSet<Prim.Edge> selectedEdges;

    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        random = new Random();
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();
        
        PlaceRooms();
        Triangulate();
        CreateHallways();
        PathfindHallways();
    }
    
    private void PlaceRooms()
    {
        // loop until we have attempted to generate roomCount rooms
        for (int i = 0; i < roomCount; i++)
        {
            // randomly generate the new room's location. Note: Random.Next(inclusive, exclusive)
            Vector2Int location = new Vector2Int(
                random.Next(0, size.x),
                random.Next(0, size.y));
            
            // randomly generate the new room's size
            Vector2Int roomSize = new Vector2Int(
                random.Next(1, roomMaxSize.x + 1),
                random.Next(1, roomMaxSize.y + 1));

            bool add = true;
            Room newRoom = new Room(location, roomSize);
            
            // buffer space around the room so rooms don't touch
            Room buffer = new Room(location + new Vector2Int(-1, -1), roomSize + new Vector2Int(2, 2));

            // validate that rooms have been generated in empty space and don't overlap
            foreach (Room room in rooms)
            {
                if (Room.Intersect(room, buffer))
                {
                    add = false;
                    break;
                }
            }

            // validate that the room didn't generate outside the boundary
            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x || newRoom.bounds.yMin < 0 ||
                newRoom.bounds.yMax >= size.y)
            {
                add = false;
            }

            // if the room is a valid room, add it to our list of rooms and generate it in worldspace
            if (add)
            {
                rooms.Add(newRoom);
                PlaceRoom(newRoom.bounds.position, newRoom.bounds.size);

                foreach (var pos in newRoom.bounds.allPositionsWithin) {
                    grid[pos] = CellType.Room;
                }
            }
        }
    }

    // generates a Delaunay graph connecting the rooms together
    private void Triangulate()
    {
        List<Vertex> vertices = new List<Vertex>();

        // add a vertex at the center of each room
        foreach (Room room in rooms)
        {
            vertices.Add(new Vertex<Room>((Vector2)room.bounds.position + ((Vector2)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay2D.Triangulate(vertices);
    }

    private void CreateHallways()
    {
        List<Prim.Edge> edges = new List<Prim.Edge>();

        foreach (var edge in delaunay.Edges)
        {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);

        selectedEdges = new HashSet<Prim.Edge>(mst);
        HashSet<Prim.Edge> remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (Prim.Edge edge in remainingEdges)
        {
            if (random.NextDouble() < 0.125)
            {
                selectedEdges.Add(edge);
            }
        }
    }

    private void PathfindHallways()
    {
        DungeonPathfinder2D aStar = new DungeonPathfinder2D(size);

        foreach (Edge edge in selectedEdges)
        {
            Room startRoom = (edge.U as Vertex<Room>).Item;
            Room endRoom = (edge.V as Vertex<Room>).Item;

            Vector2 startPosf = startRoom.bounds.center;
            Vector2 endPosf = endRoom.bounds.center;
            Vector2Int startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
            Vector2Int endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);

            var path = aStar.FindPath(startPos, endPos,
                (DungeonPathfinder2D.Node a, DungeonPathfinder2D.Node b) =>
                {
                    var pathCost = new DungeonPathfinder2D.PathCost();

                    pathCost.cost = Vector2Int.Distance(b.Position, endPos); // heuristic

                    if (grid[b.Position] == CellType.Room)
                    {
                        pathCost.cost += 10;
                    }
                    else if (grid[b.Position] == CellType.None)
                    {
                        pathCost.cost += 5;
                    }
                    else if (grid[b.Position] == CellType.Hallway)
                    {
                        pathCost.cost += 1;
                    }

                    pathCost.traversable = true;

                    return pathCost;
                });

            if (path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    var current = path[i];

                    if (grid[current] == CellType.None)
                    {
                        grid[current] = CellType.Hallway;
                    }

                    if (i > 0)
                    {
                        var prev = path[i - 1];
                        var delta = current - prev;
                    }
                }

                foreach (var pos in path)
                {
                    if (grid[pos] == CellType.Hallway)
                    {
                        PlaceHallway(pos);
                    }
                }
            }
        }
    }
    

    // place a cube of the appropriate size and color in the room's location, for testing purposes
    private void PlaceCube(Vector2Int location, Vector2Int size, Material material)
    {
        GameObject go = Instantiate(cubePrefab, new Vector3(location.x, 0, location.y), Quaternion.identity);
        go.GetComponent<Transform>().localScale = new Vector3(size.x, 1, size.y);
        go.GetComponent<MeshRenderer>().material = material;
    }
    
    // place the room into worldspace
    private void PlaceRoom(Vector2Int location, Vector2Int size)
    {
        PlaceCube(location, size, redMaterial);
    }

    private void PlaceHallway(Vector2Int location)
    {
        PlaceCube(location, new Vector2Int(1, 1), blueMaterial);
    }

    private void OnDrawGizmos()
    {
        
        if (delaunay != null)
        {
            Gizmos.color = Color.blue;
            foreach (Prim.Edge edge in selectedEdges)
            {
                Gizmos.DrawLine(new Vector3(edge.U.Position.x, 1f, edge.U.Position.y), 
                    new Vector3(edge.V.Position.x, 1f, edge.V.Position.y));
            }
            
            Gizmos.color = Color.green;
            HashSet<Prim.Edge> unusedEdges = new HashSet<Prim.Edge>();
            foreach (var edge in delaunay.Edges)
            {
                unusedEdges.Add(new Prim.Edge(edge.U, edge.V));
            }
            unusedEdges.ExceptWith(selectedEdges);
            
            foreach (Prim.Edge edge in unusedEdges)
            {
                Gizmos.DrawLine(new Vector3(edge.U.Position.x, 1f, edge.U.Position.y), 
                    new Vector3(edge.V.Position.x, 1f, edge.V.Position.y));
            }
        }
    }
}
