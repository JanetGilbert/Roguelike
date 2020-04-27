using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;



public class CreateLevel : MonoBehaviour
{
    public Tile wall;
    public Tile floor;
    public Tile door;

    public int maxRooms;

    public TileDefinitions tileDefs;

    private LevelMap levelMap;

    private Tilemap tilemap;



    void Start()
    {
        levelMap = new LevelMap();
        tilemap = GetComponentInChildren<Tilemap>();

        levelMap.Init(30, 30, 5);

        DrawGridFromMap();

        
    }

    
    void Update()
    {
        
    }

    private void DrawGridFromMap()
    {
        for (int x = 0; x < levelMap.GridW; x++)
        {
            for (int y = 0; y < levelMap.GridH; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), tileDefs.GetTileFromType(levelMap.GetTileType(x, y)));
            }
        }
    }
}
