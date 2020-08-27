using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorGen : MonoBehaviour
{
    [SerializeField] private GameObject floorTile;
    [SerializeField] private GameObject doorway;

    [SerializeField] private int startingRoomWidth;
    [SerializeField] private int startingRoomLength;
    void Start()
    {
        for (int i = 0; i < startingRoomWidth; i++)
        {
            for (int j = 0; j < startingRoomLength; j++)
            {
                Instantiate(floorTile, new Vector3(i, 0f, j), Quaternion.identity);
            }
        }

        int maxDoors = Random.Range(1, 4);
        for (int i = 0; i < maxDoors; i++)
        {
            Instantiate(doorway, new Vector3(startingRoomWidth / (i + 1), 0f, 0f), Quaternion.identity);
        }
    }
}
