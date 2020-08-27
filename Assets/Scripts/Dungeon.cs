using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dungeon : MonoBehaviour
{
    private Dictionary<Vector3, Cell> Cells = new Dictionary<Vector3, Cell>();

    [SerializeField] private GameObject floorCellGameObject;
    [SerializeField] private GameObject wallCellGameObject;
    [SerializeField] private GameObject structureGameObject;
    [SerializeField] private GameObject fpsController;

    private Cell floorCell = new Cell(CellType.Floor);
    private Cell wallCell = new Cell(CellType.Wall);
    private Cell structureCell = new Cell(CellType.Structure);

    private void Start()
    {
        //GenerateDungeon();
        //InstantiateDungeon();

        //Destroy(FindObjectOfType<Camera>().gameObject);
        //Instantiate(fpsController, new Vector3(2f, 2f, 2f), Quaternion.identity);
    }

    private void GenerateDungeon()
    {
        // Rooms
        CreateRoom(new Vector3Int(0, 0, 0 ), new Vector3Int(10,10,10));    // 1
        CreateRoom(new Vector3Int(-10,0,20), new Vector3Int(40,10,25));    // 2
        
        CreateRoom(new Vector3Int(-62,0,70), new Vector3Int(30,10,20));    // 3
        CreateRoom(new Vector3Int(-22,-50,65), new Vector3Int(70,70,70));  // 4
        CreateRoom(new Vector3Int(70,0,70), new Vector3Int(30,20,20));     // 5
        
        CreateRoom(new Vector3Int(-60, 0,175), new Vector3Int(12,10,12));   // 6
        CreateRoom(new Vector3Int(-30, -70, 150), new Vector3Int(90, 150, 95)); // 7
        CreateRoom(new Vector3Int(70,0,110), new Vector3Int(40,10,70));    // 8
        
        // Doorways
        CreatePassageway(new Vector3Int(2,1,10));
        CreatePassageway(new Vector3Int(2,1,19));
        
        // Features
        // Entry Stairs
        CreateStructure(new Vector3Int(1,1,1), new Vector3Int( 2,2,2));
        
        //Exit Stairs
        CreateStructure(new Vector3Int(-58,1,181), new Vector3Int(3,2,2));
        
        // Room 4
        CreateStructure(new Vector3Int(-22, 0, 65), new Vector3Int(70, 1, 35));
        CreateStructure(new Vector3Int(-2, 0, 100), new Vector3Int(4, 1, 35));
        
        // Room 5 - Goblin Nest
        CreateStructure(new Vector3Int(84, 10, 74), new Vector3Int(6,6,6));
        
        // Hallways
        CreateStructure(new Vector3Int(2,0,10), new Vector3Int(1,1,9));
    }

    private void InstantiateDungeon()
    {
        foreach (KeyValuePair<Vector3, Cell> kvp in Cells)
        {
            switch (kvp.Value.Type)
            {
                case CellType.Floor:
                    Instantiate(floorCellGameObject, kvp.Key, Quaternion.identity);
                    break;
                case CellType.Wall:
                    Instantiate(wallCellGameObject, kvp.Key, Quaternion.identity);
                    break;
                case CellType.Structure:
                    Instantiate(structureGameObject, kvp.Key, Quaternion.identity);
                    break;
                default:
                    Debug.Log("Unknown CellType at " + kvp.Key);
                    break;
            }
        }
    }

    private void CreateRoom(Vector3Int offset, Vector3Int size)
    {
        for (int x = offset.x + 1; x < offset.x + size.x; x++)
        {
            for (int z = offset.z + 1; z < offset.z + size.z; z++)
            {
                Vector3 NewFloorCellLocation = new Vector3(x, offset.y, z);
                if (!Cells.ContainsKey(NewFloorCellLocation))
                {
                    Cells.Add(NewFloorCellLocation, floorCell);
                }
            }
        }

        for (int x = offset.x; x <= offset.x + size.x; x++)
        {
            for (int z = offset.z; z <= offset.z + size.z; z++)
            {
                Vector3 floorReferenceLocation = new Vector3(x, offset.y, z);
                if (!Cells.ContainsKey(floorReferenceLocation))
                {
                    for (int y = offset.y + 1; y < offset.y + size.y; y++)
                    {
                        Vector3 newWallCellLocation = new Vector3(x, y, z);
                        if (!Cells.ContainsKey(newWallCellLocation))
                        {
                            Cells.Add(newWallCellLocation, wallCell);
                        }
                    }
                }
            }
        }
    }

    private void CreatePassageway(Vector3Int offset)
    {
        for (int x = offset.x; x < offset.x + 2; x++)
        {
            for (int z = offset.z; z < offset.z + 2; z++)
            {
                for (int y = offset.y; y < offset.y + 2; y++)
                {
                    Cells.Remove(new Vector3(x, y, z));
                }

                if (!Cells.ContainsKey(new Vector3(x, offset.y - 1, z)))
                {
                    Cells.Add(new Vector3(x, offset.y - 1, z), floorCell);
                }
            }
        }
    }

    private void CreateStructure(Vector3Int offset, Vector3Int size)
    {
        for (int x = offset.x; x <= offset.x + size.x; x++)
        {
            for (int y = offset.y; y < offset.y + size.y; y++)
            {
                for (int z = offset.z; z <= offset.z + size.z; z++)
                {
                    Vector3 newStructureCellLocation = new Vector3(x, y, z);

                    if (!Cells.ContainsKey(newStructureCellLocation))
                    {
                        Cells.Add(newStructureCellLocation, structureCell);
                    }
                }
            }
        }
    }

}
