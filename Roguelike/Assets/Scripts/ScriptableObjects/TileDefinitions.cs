using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public struct TileDef
{
    public LevelTileType type;
    public Tile tile; 
}

[CreateAssetMenu(fileName = "TileDefinitions", menuName = "ScriptableObjects/TileDefinitions", order = 1)]
public class TileDefinitions : ScriptableObject
{
    [SerializeField]
    public TileDef[] tileDefs;


    public Tile GetTileFromType(LevelTileType type)
    {
        foreach (TileDef t in tileDefs)
        {
            if (t.type == type)
            {
                return t.tile;
            }
        }

        return tileDefs[0].tile;
    }
}
