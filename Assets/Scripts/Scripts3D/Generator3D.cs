using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;
using UnityEngine.Events;

public class Generator3D : MonoBehaviour
{
    enum CellType
    {
        None,
        Room,
        Hallway,
        Stairs
    }

    class Room
    {
        // the boundary of our Room in worldspace
        public BoundsInt bounds;

        public Room(Vector3Int location, Vector3Int size)
        {
            bounds = new BoundsInt(location, size);
        }

        // checks if two rooms overlap
        public static bool Intersect(Room a, Room b)
        {
            return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) 
                     || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x) 
                     || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) 
                     || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y)
                     || (a.bounds.position.z >= (b.bounds.position.z + b.bounds.size.z)) 
                     || ((a.bounds.position.z + a.bounds.size.z) <= b.bounds.position.z));
        }
    }

    // the size of the area the rooms can be generated in
    [SerializeField] private Vector3Int size;

    // number of tries to generate rooms
    [SerializeField] private int roomCount;
    
    // maximum size a generated room can be
    [SerializeField] private Vector3Int roomMaxSize;
    
    // fields for testing the generation system
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private Material redMaterial;
    [SerializeField] private Material blueMaterial;
    [SerializeField] private Material greenMaterial;

    // reference to the random number generator
    private Random random;
    private Grid3D<CellType> grid;
    
    // a list of rooms that have been added to worldspace
    private List<Room> rooms;

    // a Delaunay graph containing all the rooms
    private Delaunay3D delaunay;
    
    public UnityEvent noValidPathEvent;

    //
    private HashSet<Prim.Edge> selectedEdges;

    /*private void Start()
    {
        Generate();
    }*/

    public void GenerateLevel()
    {
        Generate();
    }

    private void Generate()
    {
        var randomSeed = Environment.TickCount;
        random = new Random(randomSeed);
        //random = new Random(489825203);
        Debug.Log("random seed = " + randomSeed);
        grid = new Grid3D<CellType>(size, Vector3Int.zero);
        rooms = new List<Room>();
        
        PlaceRooms();
        Triangulate();
        CreateHallways();
        StartCoroutine(PathfindHallways());
    }
    
    private void PlaceRooms()
    {
        // loop until we have attempted to generate roomCount rooms
        for (int i = 0; i < roomCount; i++)
        {
            // randomly generate the new room's location. Note: Random.Next(inclusive, exclusive)
            Vector3Int location = new Vector3Int(
                random.Next(0, size.x),
                random.Next(0, size.y),
                random.Next(0, size.z));
            
            // randomly generate the new room's size
            Vector3Int roomSize = new Vector3Int(
                random.Next(1, roomMaxSize.x + 1),
                random.Next(1, roomMaxSize.y + 1),
                random.Next(1, roomMaxSize.z + 1));

            bool add = true;
            Room newRoom = new Room(location, roomSize);
            
            // buffer space around the room so rooms don't touch
            Room buffer = new Room(location + new Vector3Int(-1, 0, -1), roomSize + new Vector3Int(2, 0, 2));

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
            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x 
                || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y
                || newRoom.bounds.zMin < 0 || newRoom.bounds.zMax >= size.z)
            {
                add = false;
            }

            // if the room is a valid room, add it to our list of rooms and generate it in worldspace
            if (add)
            {
                rooms.Add(newRoom);
                PlaceRoom(newRoom.bounds.position, newRoom.bounds.size);

                foreach (var pos in newRoom.bounds.allPositionsWithin)
                {
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
            vertices.Add(new Vertex<Room>((Vector3)room.bounds.position + ((Vector3)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay3D.Triangulate(vertices);
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
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (Prim.Edge edge in remainingEdges)
        {
            if (random.NextDouble() < 0.125)
            {
                selectedEdges.Add(edge);
            }
        }
    }

    private IEnumerator PathfindHallways()
    {
        DungeonPathfinder3D aStar = new DungeonPathfinder3D(size);

        foreach (var edge in selectedEdges)
        {
            Room startRoom = (edge.U as Vertex<Room>).Item;
            Room endRoom = (edge.V as Vertex<Room>).Item;

            Vector3 startPosf = startRoom.bounds.center;
            Vector3 endPosf = endRoom.bounds.center;
            Vector3Int startPos = new Vector3Int((int)startPosf.x, (int)startPosf.y, (int)startPosf.z);
            Vector3Int endPos = new Vector3Int((int)endPosf.x, (int)endPosf.y, (int)endPosf.z);

            var path = aStar.FindPath(startPos, endPos,
                (DungeonPathfinder3D.Node a, DungeonPathfinder3D.Node b) =>
                {
                    var pathCost = new DungeonPathfinder3D.PathCost();

                    var delta = b.Position - a.Position;

                    if (delta.y == 0)
                    {
                        // flat hallway
                        pathCost.cost = Vector3Int.Distance(b.Position, endPos); // heuristic

                        if (grid[b.Position] == CellType.Stairs)
                        {
                            return pathCost;
                        }
                        else if (grid[b.Position] == CellType.Room)
                        {
                            pathCost.cost += 5;
                        }
                        else if (grid[b.Position] == CellType.None)
                        {
                            pathCost.cost += 1;
                        }

                        pathCost.traversable = true;
                    }
                    else
                    {
                        // staircase
                        if ((grid[a.Position] != CellType.None && grid[a.Position] != CellType.Hallway)
                            || (grid[b.Position] != CellType.None && grid[b.Position] != CellType.Hallway))
                            return pathCost;

                        pathCost.cost = 100 + Vector3Int.Distance(b.Position, endPos); // base cost + heuristic

                        int xDir = Mathf.Clamp(delta.x, -1, 1);
                        int zDir = Mathf.Clamp(delta.z, -1, 1);
                        Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
                        Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);

                        if (!grid.InBounds(a.Position + verticalOffset)
                            || !grid.InBounds(a.Position + horizontalOffset)
                            || !grid.InBounds(a.Position + verticalOffset + horizontalOffset))
                        {
                            return pathCost;
                        }

                        if (grid[a.Position + horizontalOffset] != CellType.None
                            || grid[a.Position + horizontalOffset * 2] != CellType.None
                            || grid[a.Position + verticalOffset + horizontalOffset] != CellType.None
                            || grid[a.Position + verticalOffset + horizontalOffset * 2] != CellType.None)
                        {
                            return pathCost;
                        }
                        
                        pathCost.traversable = true;
                        pathCost.isStairs = true;
                    }

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

                        if (delta.y != 0)
                        {
                            int xDir = Mathf.Clamp(delta.x, -1, 1);
                            int zDir = Mathf.Clamp(delta.z, -1, 1);
                            Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
                            Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);

                            grid[prev + horizontalOffset] = CellType.Stairs;
                            grid[prev + horizontalOffset * 2] = CellType.Stairs;
                            grid[prev + verticalOffset + horizontalOffset] = CellType.Stairs;
                            grid[prev + verticalOffset + horizontalOffset * 2] = CellType.Stairs;

                            yield return StartCoroutine(PlaceStairs(prev + horizontalOffset));
                            yield return StartCoroutine(PlaceStairs(prev + horizontalOffset * 2));
                            yield return StartCoroutine(PlaceStairs(prev + verticalOffset + horizontalOffset));
                            yield return StartCoroutine(PlaceStairs(prev + verticalOffset + horizontalOffset * 2));
                        }
                        
                        Debug.DrawLine(prev + new Vector3(0.5f, 0.5f, 0.5f), current + new Vector3(0.5f, 0.5f, 0.5f), Color.blue, 100, false);
                    }
                }

                foreach (var pos in path)
                {
                    if (grid[pos] == CellType.Hallway)
                    {
                        yield return StartCoroutine(PlaceHallway(pos));
                    }
                }
            }
            else // no valid path found
            {
                //noValidPathEvent.Invoke();
            }
        }
    }
    

    // place a cube of the appropriate size and color in the room's location, for testing purposes
    private void PlaceCube(Vector3Int location, Vector3Int size, Material material)
    {
        GameObject go = Instantiate(cubePrefab, location, Quaternion.identity);
        go.GetComponent<Transform>().localScale = size;
        go.GetComponent<MeshRenderer>().material = material;
    }
    
    // place the room into worldspace
    private void PlaceRoom(Vector3Int location, Vector3Int size)
    {
        PlaceCube(location, size, redMaterial);
    }

    private IEnumerator PlaceHallway(Vector3Int location)
    {
        PlaceCube(location, new Vector3Int(1, 1 , 1), blueMaterial);
        //yield return new WaitForSeconds(0.2f);
        yield return null;
    }

    private IEnumerator PlaceStairs(Vector3Int location)
    {
        PlaceCube(location, new Vector3Int(1, 1, 1), greenMaterial);
        //yield return new WaitForSeconds(0.2f);
        yield return null;
    }

   private void OnDrawGizmos()
    {
        if (delaunay != null)
        {
            
            Gizmos.color = Color.cyan;
            foreach (Prim.Edge edge in selectedEdges)
            {
                    Gizmos.DrawLine(edge.U.Position, edge.V.Position);
            }
/*            Gizmos.color = Color.green;
            HashSet<Prim.Edge> unusedEdges = new HashSet<Prim.Edge>();
            foreach (var edge in delaunay.Edges)
            {
                unusedEdges.Add(new Prim.Edge(edge.U, edge.V));
            }
            unusedEdges.ExceptWith(selectedEdges);

            foreach (Prim.Edge edge in unusedEdges)
            {
                Gizmos.DrawLine(edge.U.Position, edge.V.Position);
            }*/
            

            /*Gizmos.color = Color.green;
            foreach (var edge in delaunay.Edges)
            {
                Gizmos.DrawLine(edge.U.Position, edge.V.Position);
            }*/
        }
    }
}
