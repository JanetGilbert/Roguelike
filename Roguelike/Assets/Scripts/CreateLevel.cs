using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;



public class CreateLevel : MonoBehaviour
{
   // public Tile wall;
   // public Tile floor;
   // public Tile door;

    public int maxRooms;

    public TileDefinitions tileDefs;

    private LevelMap levelMap;

    private Tilemap tilemap;



    void Start()
    {
        levelMap = new LevelMap();
        tilemap = GetComponentInChildren<Tilemap>();


        int sizeX = 50;
        int sizeY = 30;
        levelMap.Init(sizeX, sizeY, 5, 4, 10);

        DrawGridFromMap();

        // Fit tile map to screen.
        Vector3 boardSize = tilemap.localBounds.size;
        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetRatio = boardSize.x / boardSize.y;

        if (screenRatio >= targetRatio)
        {
            Camera.main.orthographicSize = boardSize.y / 2;
        }
        else
        {
            float differenceInSize = targetRatio / screenRatio;
            Camera.main.orthographicSize = boardSize.y / 2 * differenceInSize;
        }

        // Center grid inside camera view.
        Camera.main.transform.position = new Vector3((boardSize.x * 0.5f), (boardSize.y * 0.5f), Camera.main.transform.position.z);
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


public static class Extensions
{
    // This function is added in 2020 versions of Unity.
    // Code taken from https://stackoverflow.com/questions/306316/determine-if-two-rectangles-overlap-each-other
    public static bool Overlap(this RectInt rect, RectInt other)
    {
        bool noOverlap = rect.x > other.xMax ||
                 other.x > rect.xMax ||
                 rect.y > other.yMax ||
                 other.y > rect.yMax;

        return !noOverlap;
    }

    public static RectInt Increase(this RectInt rect, int border)
    {
          return new RectInt(rect.x - border, rect.y - border,
                             (rect.width + border * 2),
                             (rect.height + border * 2));

    }
}