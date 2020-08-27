using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Cameras;

public enum CellType {
    Floor,
    Wall,
    Passageway,
    Structure
}

public class Cell
{
    public Cell(CellType type)
    {
        this.type = type;
    }
    
    private CellType type;
    public CellType Type => type;
}
