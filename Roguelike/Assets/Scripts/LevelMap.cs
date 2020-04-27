using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum LevelTileType { Empty = 0, Corridor = 1, Floor = 2}

public struct Room
{
    public Vector2Int pos;
    public Vector2Int size;
}

public class LevelMap
{
    private AStarTile[,] level;

    private int _gridW;
    private int _gridH;

    public int GridW
    {
        get
        {
            return _gridW;
        }
    }

    public int GridH
    {
        get
        {
            return _gridH;
        }
    }

    private const int maxRoomSize = 10;
    private const int minRoomSize = 3;

    // All possible directions. 
    protected readonly Vector2Int[] allDirections =
                                           {Vector2Int.up,
                                           // Vector2Int.up + Vector2Int.left,
                                            //Vector2Int.up + Vector2Int.right,
                                            Vector2Int.down,
                                           // Vector2Int.down + Vector2Int.left,
                                           // Vector2Int.down + Vector2Int.right,
                                            Vector2Int.left,
                                            Vector2Int.right };

    public void Init(int tilesX, int tilesY, int roomsNumber)
    {
        _gridW = tilesX;
        _gridH = tilesY;

        level = new AStarTile[GridW, GridH];

        for (int x = 0; x < GridW; x++)
        {
            for (int y = 0; y < GridH; y++)
            {
                level[x, y] = new AStarTile(new Vector2Int(x, y));
            }
        }

        List<Room> rooms = new List<Room>();
        for (int r = 0; r < roomsNumber; r++)
        {
            rooms.Add(PlaceRoom());
        }

        foreach (Room r in rooms)
        {
            Vector2Int startDoorPos = GetRoomWallPos(r);

            Room randomRoom = rooms[Random.Range(0, rooms.Count)];
            Room targetRoom = new Room();
            targetRoom.pos = new Vector2Int(randomRoom.pos.x - 1, randomRoom.pos.y - 1); // todo check on screen
            targetRoom.size = new Vector2Int(randomRoom.size.x + 1, randomRoom.size.y + 1);

            Vector2Int endDoorPos = GetRoomWallPos(targetRoom);

            bool pathFound = CalculateAStar(startDoorPos, endDoorPos, true);

            // Draw path
            if (pathFound)
            {
                AStarTile current = level[endDoorPos.x, endDoorPos.y];
                while (current.prev != null)
                {
                    current.type = LevelTileType.Corridor;
                    current = current.prev;
                }
            }
        }
    }

    private Vector2Int GetRoomWallPos(Room room)
    {
        int squares = (room.size.x + (room.size.y - 1)) * 2;
        int randSquare = Random.Range(0, squares);

        // Top
        if (randSquare < room.size.x)
        {
            return new Vector2Int(Random.Range(room.pos.x, room.pos.x + room.size.x), room.pos.y);
        }

        randSquare -= room.size.x;

        // Bottom
        if (randSquare < room.size.x)
        {
            return new Vector2Int(Random.Range(room.pos.x, room.pos.x + room.size.x), room.pos.y + room.size.y - 1);
        }

        randSquare -= room.size.x;

        // Left
        if (randSquare < room.size.y)
        {
            return new Vector2Int(room.pos.x, Random.Range(room.pos.x, room.pos.y + room.size.y));
        }

        // Right
        return new Vector2Int(room.pos.x + room.size.x - 1, Random.Range(room.pos.y, room.pos.y + room.size.y));

    }

    private Room PlaceRoom()
    {
        //int roomSizeX, roomSizeY, roomPosX, roomPosY;
        Room room = new Room();
        bool isOK;

        do
        {
            isOK = true;

            room.size.x = Random.Range(minRoomSize, maxRoomSize);
            room.size.y = Random.Range(minRoomSize, maxRoomSize);
            room.pos.x = Random.Range(0, _gridW);
            room.pos.y = Random.Range(0, _gridH);

            if (room.pos.x + room.size.x >= _gridW || room.pos.y + room.size.y >= _gridH)
            {
                isOK = false;
            }

            if (isOK)
            {
                for (int x = room.pos.x; x < room.pos.x + room.size.x; x++)
                {
                    for (int y = room.pos.y; y < room.pos.y + room.size.y; y++)
                    {
                        if (level[x, y].type != LevelTileType.Empty)
                        {
                            isOK = false;
                        }
                    }
                }
            }
        } while (!isOK);


        for (int x = room.pos.x; x < room.pos.x + room.size.x; x++)
        {
            for (int y = room.pos.y; y < room.pos.y + room.size.y; y++)
            {
                level[x, y].type = LevelTileType.Floor;
            }
        }

        return room;
    }

    public LevelTileType GetTileType(int x, int y)
    {
        return level[x, y].type;
    }


    // Find the path to the destination using the A* algorithm.
    private bool CalculateAStar(Vector2Int startPos, Vector2Int endPos, bool emptySpace)
    {
        AStarTile start = level[startPos.x, startPos.y];
        AStarTile end = level[endPos.x, endPos.y];
        List<AStarTile> open = new List<AStarTile>(); // List of tiles we need to check.
        List<AStarTile> closed = new List<AStarTile>(); // List of tiles we have already checked.
        AStarTile current = start;

        // Reset path temps
        for (int x = 0; x < GridW; x++)
        {
            for (int y = 0; y < GridH; y++)
            {
                level[x, y].ResetPath();
            }
        }

        // Add the starting tile to the open list.
        open.Add(start);
        start.prev = null;

        // Repeat until we have either checked all tiles or found the end.
        while (open.Count > 0 && !open.Contains(end))
        {
            // Find the tile on the open list with the least cost.
            int minCost = int.MaxValue;
            int lowestIndex = 0;

            for (int t = 0; t < open.Count; t++)
            {
                if (open[t].Cost < minCost)
                {
                    minCost = open[t].Cost;
                    lowestIndex = t;
                }
            }

            current = open[lowestIndex]; // Move to the tile with least cost.
            open.Remove(current); // Remove it from the open list.
            closed.Add(current); // Add it to the closed list.


            // Find all valid adjacent tiles.
            List<AStarTile> adjacent = new List<AStarTile>();

            foreach (Vector2Int dir in allDirections)
            {
                Vector2Int pos = current.pos + dir;
                if (IsValidTile(pos, emptySpace)) // Check that it is possible to move to the tile.
                {
                    if (!closed.Contains(level[pos.x, pos.y])) // Make sure the tile hasn't been already checked.
                    {
                        adjacent.Add(level[pos.x, pos.y]);
                    }
                }
            }

            // Add the best adjacent tile to the path.
            foreach (AStarTile tile in adjacent)
            {
                if (open.Contains(tile))
                {
                    // If the adjacent tile is already in the open list, and the distance is shorter via this route,
                    // set the current tile to be its "parent." 
                    if (current.distFromStart + 1 < tile.distFromStart)
                    {
                        tile.distFromStart = current.distFromStart + 1;
                        tile.prev = current;
                    }
                }
                else
                {
                    // If the adjacent tile is not in the open list, add it, and set the current tile to be its "parent." 
                    open.Add(tile);
                    tile.prev = current;
                    tile.distFromStart = current.distFromStart + 1;
                    tile.distFromEnd = Mathf.Abs(tile.pos.x - end.pos.x) + Mathf.Abs(tile.pos.y - end.pos.y);
                }
            }
        }

        // Build display path.
        /*  pathTiles = new List<Tile>();


          if (open.Contains(end))
          {
              current = end;
              while (current.prev != null)
              {
                  pathTiles.Add(current.displayTile);
                  current = current.prev;
              }

              pathTiles.Reverse(); // Reverse display path as it is built from the destination to the start.
          }*/

 
        return open.Contains(end);
    }

    // Is the position on the board, and not blocked?
    public bool IsValidTile(Vector2Int pos, bool emptySpace)
    {
        if ((pos.x >= 0) &&
            (pos.y >= 0) &&
            (pos.x < GridW) &&
            (pos.y < GridH))
        {
            if (emptySpace)
            {
                if (level[pos.x, pos.y].type == LevelTileType.Empty)
                {
                    return true;
                }
            }
            else
            {
                if (level[pos.x, pos.y].type != LevelTileType.Empty)
                {
                    return true;
                }
            }
        }

        return false;
    }


    private class AStarTile
    {
        public LevelTileType type;
        public Vector2Int pos;

        public AStarTile prev; // The previous tile in the path.
        public int distFromStart; // How far have we come from the start?
        public int distFromEnd; // A guess at how far we are from the destination.
        public int Cost // How good is this tile? The lower the better.
        {
            get
            {
                return distFromStart + distFromEnd;
            }
        }

        // Constructor
        public AStarTile(Vector2Int newPos)
        {
            pos = newPos;
            type = LevelTileType.Empty;
            ResetPath();
        }

        public void ResetPath()
        {
            prev = null;
            distFromStart = 0;
            distFromEnd = 0;
        }
    }
}


 