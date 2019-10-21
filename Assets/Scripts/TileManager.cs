using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    [SerializeField] private Material matHighlightPath;
    [SerializeField] private Material matHighlightMoveArea;
    [SerializeField] private Material matTile;
    [SerializeField] private Material matMud;
    [SerializeField] private bool diagonals = false;
    [SerializeField] private int maxMove = 3;

    private GameObject[,] tileGrid;
    private List<GameObject> moveableTiles;
    private int maxX;
    private int maxZ;

    private bool selectOn;
    private bool pathed;
    private PathFinder pathfinder;
    private GameObject selected;
    private GameObject target;
    private GameObject previousTarget;

    // Awake
    private void Awake()
    {
        tileGrid = GetTileGridFromScene();
        Dictionary<TileType, float> tilecosts = new Dictionary<TileType, float>();
        tilecosts[TileType.Default] = 1;
        tilecosts[TileType.Mud] = 2;
        pathfinder = new PathFinder(diagonals, tilecosts);
        moveableTiles = new List<GameObject>();
        selectOn = false;
        pathed = true;
        //DuplicateTiles(tileGrid);
    }

    // Duplicates all tiles to make sure tileGrid is correct
    private void DuplicateTiles(GameObject[,] tilegrid) //for testing
    {
        for (int i = 0; i < maxX; i++)
        {
            for (int j = 0; j < maxZ; j++)
            {
                GameObject temp = Instantiate(tilegrid[i, j]);
                temp.transform.position = new Vector3(tilegrid[i, j].transform.position.x + maxX, tilegrid[i, j].transform.position.y, tilegrid[i, j].transform.position.z);
            }
        }
    }

    // Fills tileGrid array with the placed tiles in scene
    private GameObject[,] GetTileGridFromScene()
    {
        Queue<GameObject> toAdd = new Queue<GameObject>();
        maxX = 0;
        maxZ = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            toAdd.Enqueue(transform.GetChild(i).gameObject);
            maxX = (transform.GetChild(i).gameObject.transform.position.x > maxX) ? (int)transform.GetChild(i).gameObject.transform.position.x : maxX;
            maxZ = (transform.GetChild(i).gameObject.transform.position.z > maxZ) ? (int)transform.GetChild(i).gameObject.transform.position.x : maxZ;
        }

        maxX++;
        maxZ++;

        GameObject[,] tempGrid = new GameObject[maxX, maxZ];
        while (toAdd.Count != 0)
        {
            tempGrid[(int)toAdd.Peek().transform.position.x, (int)toAdd.Peek().transform.position.z] = toAdd.Dequeue();
        }

        return tempGrid;
    }

    // Resets all tiles and highlights
    private void ResetTiles(bool resetAll)
    {
        for (int i = 0; i < maxX; i++)
        {
            for (int j = 0; j < maxZ; j++)
            {
                if (resetAll)
                {
                    if (tileGrid[i, j].tag == "Tile")
                        tileGrid[i, j].GetComponent<MeshRenderer>().material = matTile;
                    else if (tileGrid[i, j].tag == "Mud")
                        tileGrid[i, j].GetComponent<MeshRenderer>().material = matMud;
                } else
                {
                    if (tileGrid[i, j].GetComponent<MeshRenderer>().sharedMaterial == matHighlightPath)
                        tileGrid[i, j].GetComponent<MeshRenderer>().material = matHighlightMoveArea;
                }
            }
        }
    }

    // Highlights path
    private void HighlightPath(Queue<Vector3> path)
    {
        while (path.Count != 0)
        {
            Vector3 temp = path.Dequeue();
            HighlightTile(tileGrid[(int)temp.x, (int)temp.z], matHighlightPath);
            Debug.Log(temp);
        }
    }

    // Highlights path
    private void HighlightMoveableArea(Queue<Vector3> path)
    {
        int counter = 0;
        moveableTiles = new List<GameObject>();

        while (path.Count != 0)
        {
            Vector3 temp = path.Dequeue();
            moveableTiles.Add(tileGrid[(int)temp.x, (int)temp.z]);
            HighlightTile(moveableTiles[counter], matHighlightMoveArea);

            counter++;
        }
    }

    // Highlights tile in argument
    private void HighlightTile(GameObject tile, Material mat)
    {
        tile.GetComponent<MeshRenderer>().material = mat;
    }

    // Update is called once per frame
    private void Update()
    {
        RaycastHit hit;

        if (Input.GetMouseButtonDown(0) && !selectOn)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                selectOn = SelectStartByPosition(hit.transform.position);
            }
        }

        if (Input.GetMouseButton(1) && selectOn)
        {
            ResetAndDeselect();
        }

        if (selectOn)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                SelectTargetByPosition(hit.transform.position);
            }

            if (!pathed)
            {
                ResetTiles(false);
                HighlightPath(pathfinder.GetPath(selected.transform.position, target.transform.position));
                previousTarget = target;
                pathed = true;
            }
        }
    }

    // Sets the target if original target is intraversable tile
    private GameObject FixTarget(GameObject t)
    {
        while (t.tag == "Wall")
        {
            if (t.transform.position.x < selected.transform.position.x)
            {
                t = tileGrid[(int)t.transform.position.x + 1, (int)t.transform.position.z];
            }
            else if (t.transform.position.x > selected.transform.position.x)
            {
                t = tileGrid[(int)t.transform.position.x - 1, (int)t.transform.position.z];
            }
            else if (t.transform.position.z < selected.transform.position.z)
            {
                t = tileGrid[(int)t.transform.position.x, (int)t.transform.position.z + 1];
            }
            else if (t.transform.position.z > selected.transform.position.z)
            {
                t = tileGrid[(int)t.transform.position.x, (int)t.transform.position.z - 1];
            }
        }

        return t;
    }

    // Selects starting tile by position, returns false if fail
    public bool SelectStartByPosition(Vector3 position)
    {
        Debug.Log(position);
        if (tileGrid[(int)position.x, (int)position.z].tag != "Wall")
        {
            selected = tileGrid[(int)position.x, (int)position.z];
            HighlightMoveableArea(pathfinder.GetMoveableArea(selected.transform.position, maxMove));
            HighlightTile(selected, matHighlightPath);
            return true;
        }
        return false;
    }

    // Selects target tile by position, returns false if fail
    public bool SelectTargetByPosition(Vector3 position)
    {
        if (selectOn)
        {
            if (!moveableTiles.Contains(tileGrid[(int)position.x, (int)position.z]) || tileGrid[(int)position.x, (int)position.z].tag == "Wall")
            {
                ResetTiles(false);
                previousTarget = null;
                return false;
            }
            target = tileGrid[(int)position.x, (int)position.z];
            if (target.tag == "Wall") target = FixTarget(target);
            if (target != previousTarget)
            {
                pathed = false;
                return true;
            }
        }
        return false;
    }

    // Resets tile highlights and deselects
    public void ResetAndDeselect()
    {
        ResetTiles(true);
        selectOn = false;
    }

    // Gets a tile at position
    public GameObject GetTile(Vector3 pos)
    {
        return tileGrid[Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.z)];
    }

    // Gets the tileGrid
    public GameObject[,] GetTileGrid()
    {
        return tileGrid;
    }
}