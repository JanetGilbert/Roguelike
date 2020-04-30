using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum LevelTileType { Empty = 0, Corridor = 1, Floor = 2, Door = 3, Wall = 4}


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

    private const int maxRoomSize = 10; // todo vary
    private const int minRoomSize = 4;

    // All possible directions. 
    protected readonly Vector2Int[] allDirections =
                                           {Vector2Int.up,
                                            Vector2Int.down,
                                            Vector2Int.left,
                                            Vector2Int.right };

    public bool Init(int tilesX, int tilesY, int roomsNumber, int minRoomSize, int maxRoomSize)
    {
        // Create Tile Path grid
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

        // Create rooms
        List<RectInt> roomRects = new List<RectInt>();
        int tries = 50;


        while (tries > 0 && roomRects.Count < roomsNumber)
        {
            RectInt roomRect = MakeRoomRect(minRoomSize, maxRoomSize);
            bool overlap = false;

            for (int compareRoom = 0; compareRoom < roomRects.Count; compareRoom++)
            {
                if (roomRect.Overlap(roomRects[compareRoom].Increase(1))) 
                {
                    overlap = true;
                }
            }

            if (!overlap)
            {
                roomRects.Add(roomRect);
            }
        }

        if (roomRects.Count == 0)
        {
            return false; // No rooms generated.
        }

        // Write rooms to grid.
        foreach (RectInt rect in roomRects)
        {
            DrawRect(rect, LevelTileType.Wall);
            DrawRect(new RectInt(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), LevelTileType.Floor);
        }

        // Create paths.
        /*if (roomRects.Count > 1)
        {
            foreach (RectInt r in roomRects)
            {
                Vector2Int startPos = RandomRectPosition(r);
                RectInt otherRect = roomRects[Random.Range(0, roomRects.Count)];

                while (otherRect.position == r.position)
                {
                    otherRect = roomRects[Random.Range(0, roomRects.Count)];
                }

                Vector2Int endPos = FindNearestPointOnRect(startPos, otherRect);

                if (IsValidTile(startPos) && IsValidTile(endPos))
                {
                    if (CalculateAStar(startPos, endPos))
                    {
                        DrawPathLine(level[endPos.x, endPos.y]);
                    }
                }
            }
        }*/

        for (int r = 0; r < roomRects.Count; r++)
        {
            int rNext = r + 1;
            if (rNext >= roomRects.Count)
            {
                rNext = 0;
            }

            RectInt curRect = roomRects[r];
            RectInt nextRect = roomRects[rNext];

           // Vector2Int startPos = RandomRectPosition(curRect);
           // Vector2Int endPos = FindNearestPointOnRect(startPos, nextRect);

            Vector2Int startPos, endPos;
            FindNearestPoints(curRect, nextRect, out startPos, out endPos);

            if (IsValidTile(startPos) && IsValidTile(endPos))
            {
                if (CalculateAStar(startPos, endPos))
                {
                    DrawPathLine(level[endPos.x, endPos.y]);

                    // Draw doors.
                    Vector2Int startDoorPos = FindDoorPos(curRect, startPos);
                    Vector2Int endDoorPos = FindDoorPos(nextRect, endPos);

                    level[startDoorPos.x, startDoorPos.y].type = LevelTileType.Door;
                    level[endDoorPos.x, endDoorPos.y].type = LevelTileType.Door;
                }
            }



        }



        return true;
    }

    // Given a position next to a wall, find the place on the wall to put the door.
    private Vector2Int FindDoorPos(RectInt roomWall, Vector2Int pathStart)
    {
        foreach (Vector2Int dir in allDirections)
        {
            Vector2Int pos = pathStart + dir;

            if (IsTileOnBoard(pos))
            {
                if (level[pos.x, pos.y].type == LevelTileType.Wall)
                {
                    return pos;
                }
            }
        }

        return pathStart;
    }

    // Generate room rect.
    private RectInt MakeRoomRect(int minRoomSize, int maxRoomSize)
    {
        int sizeX = Random.Range(minRoomSize, maxRoomSize);
        int sizeY = Random.Range(minRoomSize, maxRoomSize);
        int posX = Random.Range(0, GridW - sizeX);
        int posY = Random.Range(0, GridH - sizeY);

        return new RectInt(posX, posY, sizeX, sizeY);

    }

    // Generate a random position on a rectangle's edge, avoiding corners.
    private Vector2Int RandomRectPosition(RectInt rect)
    {
        Vector2Int pos = new Vector2Int(rect.x, rect.y);
        int numPositions = (rect.width - 2) + (rect.height - 2);
        int randPos = Random.Range(0, numPositions);
        int curPos = 0;

        // Horizontal
        for (int x = rect.x + 1; x < rect.xMax - 1; x++)
        {
            if (curPos == randPos)
            {
                return new Vector2Int(x, Random.Range(0, 2) == 0? rect.y-1: rect.y+rect.height);               
            }
            curPos++;
        }

        // Vertical
        for (int y = rect.y + 1; y < rect.yMax - 1; y++)
        {
            if (curPos == randPos)
            {
                return new Vector2Int(Random.Range(0, 2) == 0 ? rect.x-1 : rect.x + rect.width, y);
            }
            curPos++;
        }


        return new Vector2Int(0,0); // Should never get here
    }

    // Find the nearest point to a rectangle from a starting point.
    private Vector2Int FindNearestPointOnRect(Vector2Int pos, RectInt rect)
    {
        int nearestX = 0;
        int nearestY = 0;
        int nearestVal = int.MaxValue;

        for (int x = rect.x + 1; x < rect.xMax - 1; x++)
        {
            int y1 = rect.y - 1;
            int y2 = rect.y + rect.height;

            int val1 = Mathf.Abs(pos.x - x) + Mathf.Abs(pos.y - y1);

            if (val1 < nearestVal)
            {
                nearestX = x;
                nearestY = y1;
                nearestVal = val1;
            }


            int val2 = Mathf.Abs(pos.x - x) + Mathf.Abs(pos.y - y2);

            if (val2 < nearestVal)
            {
                nearestX = x;
                nearestY = y2;
                nearestVal = val2;
            }
        }


        for (int y = rect.y + 1; y < rect.yMax - 1; y++)
        {
            int x1 = rect.x - 1;
            int x2 = rect.x + rect.width;

            int val1 = Mathf.Abs(pos.x - x1) + Mathf.Abs(pos.y - y);

            if (val1 < nearestVal)
            {
                nearestX = x1;
                nearestY = y;
                nearestVal = val1;
            }


            int val2 = Mathf.Abs(pos.x - x2) + Mathf.Abs(pos.y - y);

            if (val2 < nearestVal)
            {
                nearestX = x2;
                nearestY = y;
                nearestVal = val2;
            }
        }

        return new Vector2Int(nearestX, nearestY);
    }

    // Find the two closest points on two rectangles.
    private void FindNearestPoints(RectInt r1, RectInt r2, out Vector2Int foundPos1, out Vector2Int foundPos2)
    {
        Vector2Int minPos1 = new Vector2Int(r1.x, r1.y);
        Vector2Int minPos2 = new Vector2Int(r2.x, r2.y);
        int minVal = int.MaxValue;

        // Horizontal
        for (int x = r1.x + 1; x < r1.xMax - 1; x++)
        {
            Vector2Int pos1 = new Vector2Int(x, r1.y - 1);
            Vector2Int pos2 = FindNearestPointOnRect(pos1, r2);
            int val = Mathf.Abs(pos2.x - pos1.x) + Mathf.Abs(pos2.y - pos1.y);

            if (val < minVal)
            {
                minVal = val;
                minPos1 = pos1;
                minPos2 = pos2;
            }
        }
        for (int x = r1.x + 1; x < r1.xMax - 1; x++)
        {
            Vector2Int pos1 = new Vector2Int(x, r1.y + r1.height);
            Vector2Int pos2 = FindNearestPointOnRect(pos1, r2);
            int val = Mathf.Abs(pos2.x - pos1.x) + Mathf.Abs(pos2.y - pos1.y);

            if (val < minVal)
            {
                minVal = val;
                minPos1 = pos1;
                minPos2 = pos2;
            }
        }

        // Vertical
        for (int y = r1.y + 1; y < r1.yMax - 1; y++)
        {
            Vector2Int pos1 = new Vector2Int(r1.x - 1, y);
            Vector2Int pos2 = FindNearestPointOnRect(pos1, r2);
            int val = Mathf.Abs(pos2.x - pos1.x) + Mathf.Abs(pos2.y - pos1.y);

            if (val < minVal)
            {
                minVal = val;
                minPos1 = pos1;
                minPos2 = pos2;
            }
        }
        for (int y = r1.y + 1; y < r1.yMax - 1; y++)
        {
            Vector2Int pos1 = new Vector2Int(r1.x + r1.width, y);
            Vector2Int pos2 = FindNearestPointOnRect(pos1, r2);
            int val = Mathf.Abs(pos2.x - pos1.x) + Mathf.Abs(pos2.y - pos1.y);

            if (val < minVal)
            {
                minVal = val;
                minPos1 = pos1;
                minPos2 = pos2;
            }
        }

        foundPos1 = minPos1;
        foundPos2 = minPos2;
    }


    // Can this rectangle be placed?
    private bool CheckRectBlocked(RectInt rect)
    {
        for (int x = rect.x; x < rect.x + rect.width; x++)
        {
            for (int y = rect.y; x < rect.y + rect.height; y++)
            {
                if (level[x, y].type != LevelTileType.Empty)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Draw a rectangle to the board.
    public void DrawRect(RectInt rect, LevelTileType tileType)
    {
        for (int x = rect.x; x < rect.x + rect.width; x++)
        {
            for (int y = rect.y; y < rect.y + rect.height; y++)
            {
                level[x, y].type = tileType;
                
            }
        }
    }

    // Draw a line to the board.
    private void DrawPathLine(AStarTile endTile)
    {
        AStarTile curTile = endTile;

        while (curTile != null)
        {
            level[curTile.pos.x, curTile.pos.y].type = LevelTileType.Corridor;
            curTile = curTile.prev;
        }
    }

    public LevelTileType GetTileType(int x, int y)
    {
        return level[x, y].type;
    }


    // Find the path to the destination using the A* algorithm.
    private bool CalculateAStar(Vector2Int startPos, Vector2Int endPos)
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
                if (IsValidTile(pos)) // Check that it is possible to move to the tile.
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

 
        return open.Contains(end);
    }

    // Is the position on the board and not blocked?
    public bool IsValidTile(Vector2Int pos)
    {
        if ((pos.x >= 0) &&
            (pos.y >= 0) &&
            (pos.x < GridW) &&
            (pos.y < GridH))
        {
            if (level[pos.x, pos.y].type == LevelTileType.Empty)
            {
                return true;
            }
        }

        return false;
    }

    // Is the position on the board
    public bool IsTileOnBoard(Vector2Int pos)
    {
        if ((pos.x >= 0) &&
            (pos.y >= 0) &&
            (pos.x < GridW) &&
            (pos.y < GridH))
        {
            return true;
        }

        return false;
    }

    // Helper class for A* algorithm.
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


 