using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinder
{
    private GameObject[,] tileGrid;
    private Dictionary<TileType, float> tilekey;
    private TileType tiletype;
    bool diagonals;
    int maxX, maxZ;

    // Constructor takes in tilegrid and diagonal
    public PathFinder(bool diag, Dictionary<TileType, float> key)
    {
        tileGrid = GameObject.FindGameObjectWithTag("TileManager").GetComponent<TileManager>().GetTileGrid();
        diagonals = diag;

        tilekey = key;
        maxX = tileGrid.GetLength(0);
        maxZ = tileGrid.GetLength(1);
    }

    // Assigns creates tileNodes 2D array
    private TileNode[,] AssignNodes()
    {
        TileNode[,] tileNodes = new TileNode[maxX, maxZ];

        for (int i = 0; i < maxX; i++)
        {
            for (int j = 0; j < maxZ; j++)
            {
                tileNodes[i, j] = new TileNode(tileGrid[i, j].tag == "Tile" || tileGrid[i, j].tag == "Mud", i, j);
            }
        }

        return tileNodes;
    }

    // Creates path from one tile to another
    private Queue<Vector3> CreatePath(Vector3 from, Vector3 to)
    {
        int xPos = (int)from.x;
        int zPos = (int)from.z;
        int targetXPos = (int)to.x;
        int targetZPos = (int)to.z;

        TileNode[,] tileNodes = AssignNodes();
        TileNode start = tileNodes[xPos, zPos];
        TileNode target = tileNodes[targetXPos, targetZPos];

        List<TileNode> open = new List<TileNode>();
        HashSet<TileNode> closed = new HashSet<TileNode>();
        open.Add(start);

        while (open.Count > 0)
        {
            TileNode current = open[0];
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].fCost < current.fCost || open[i].fCost == current.fCost && open[i].hCost < current.hCost)
                {
                    current = open[i];
                }
            }

            open.Remove(current);
            closed.Add(current);

            if (current == target)
            {
                return RetracePath(start, target); ;
            }

            foreach (TileNode neighbor in GetNeighbors(tileNodes, current))
            {
                if (!neighbor.walkable || closed.Contains(neighbor))
                {
                    continue;
                }

                float mod = 1;
                if (tileGrid[neighbor.x, neighbor.z].tag == "Mud") mod = tilekey[TileType.Mud];
                float newMovementCost = current.gCost + GetDistance(current, neighbor) * mod;
                if (newMovementCost < neighbor.gCost || !open.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCost;
                    neighbor.hCost = GetDistance(neighbor, target);
                    neighbor.parent = current;

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }
        }

        return null;
    }

    // Retraces the path from end to start by going through the parents
    private Queue<Vector3> RetracePath(TileNode start, TileNode end)
    {
        List<TileNode> nodePath = new List<TileNode>();
        TileNode current = end;

        while (current != start)
        {
            nodePath.Add(current);
            current = current.parent;
        }

        nodePath.Reverse();

        Queue<Vector3> path = new Queue<Vector3>();
        path.Enqueue(new Vector3(start.x, 0, start.z));

        foreach (TileNode node in nodePath)
        {
            path.Enqueue(new Vector3(node.x, 0, node.z));
        }

        return path;
    }

    // Calculates the "distance" between two tiles
    private int GetDistance(TileNode a, TileNode b)
    {
        int distX = Mathf.Abs(a.x - b.x);
        int distZ = Mathf.Abs(a.z - b.z);

        if (diagonals)
        {
            if (distX > distZ)
            {
                return 14 * (distZ) + 10 * (distX - distZ);
            }

            return 14 * (distX) + 10 * (distZ - distX);
        }
        else
        {
            if (distX > distZ)
            {
                return 10 * (distZ) + 10 * (distX);
            }

            return 10 * (distX) + 10 * (distZ);
        }
    }

    // Gets neighbors of current node
    private List<TileNode> GetNeighbors(TileNode[,] nodes, TileNode node)
    {
        List<TileNode> neighbors = new List<TileNode>();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (diagonals)
                {
                    if (i == 0 && j == 0)
                    {
                        continue;
                    }
                }
                else
                {
                    if (i == 0 && j == 0 || Mathf.Abs(i) == 1 && Mathf.Abs(j) == 1)
                    {
                        continue;
                    }
                }

                int tempX = node.x + i;
                int tempZ = node.z + j;
                if (tempX >= 0 && tempX < maxX && tempZ >= 0 && tempZ < maxZ)
                {
                    neighbors.Add(nodes[tempX, tempZ]);
                }
            }
        }
        return neighbors;
    }

    // Gets moveable area based on maxmove of selected
    public Queue<Vector3> GetMoveableArea(Vector3 from, int maxmove)
    {
        Queue<Vector3> moveableTiles = new Queue<Vector3>();
        int minXClamp = (int)Mathf.Clamp(from.x - maxmove, 0, maxX);
        int maxXClamp = (int)Mathf.Clamp(from.x + maxmove + 1, 0, maxX);
        int minZClamp = (int)Mathf.Clamp(from.z - maxmove, 0, maxZ);
        int maxZClamp = (int)Mathf.Clamp(from.z + maxmove + 1, 0, maxZ);

        for (int i = minXClamp; i < maxXClamp; i++)
        {
            for (int j = minZClamp; j < maxZClamp; j++)
            {
                if (i == (int)from.x && j == (int)from.z || moveableTiles.Contains(new Vector3(i, 0, j)) || tileGrid[i, j].tag == "Wall")
                {
                    continue;
                }

                Queue<Vector3> tempQueue = GetPath(from, tileGrid[i, j].transform.position);
                int moveFloor = 0;
                if (!diagonals)
                {
                    if (tempQueue.Count - 1 <= maxmove)
                    {
                        while (tempQueue.Count != 0)
                        {
                            Vector3 temp = tempQueue.Dequeue();
                            if (tileGrid[(int)temp.x, (int)temp.z].tag == "Mud") moveFloor++;
                            if (!moveableTiles.Contains(temp))
                                moveableTiles.Enqueue(temp);
                        }
                        Debug.Log(i + " " + j);
                    }
                } else
                {
                    float moveCount = 0;
                    Vector3 prevstep = tempQueue.Peek();
                    foreach(Vector3 step in tempQueue)
                    {
                        if (prevstep == step)
                            continue;

                        if (Mathf.Abs(prevstep.x - step.x) == 1 && (Mathf.Abs(prevstep.z - step.z) == 1))
                            moveCount += 1.5f;
                        else moveCount++;

                        prevstep = step;
                    }
                    if (moveCount <= maxmove)
                    {
                        while (tempQueue.Count != 0)
                        {
                            Vector3 temp = tempQueue.Dequeue();
                            if (!moveableTiles.Contains(temp))
                                moveableTiles.Enqueue(temp);
                        }
                    }
                }
            }
        }

        Debug.Log(moveableTiles.Count);
        return moveableTiles;
    }

    // Gets the path array
    public Queue<Vector3> GetPath(Vector3 from, Vector3 to)
    {
        return CreatePath(from, to);
    }

    // Tile node for A* pathing
    private class TileNode
    {
        public bool walkable;
        public float gCost;
        public float hCost;
        public float fCost { get { return gCost + hCost; } }
        public TileNode parent;
        public int x;
        public int z;

        public TileNode(bool walk, int posX, int posZ)
        {
            walkable = walk;
            x = posX;
            z = posZ;
            parent = null;
        }
    }
}
