using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class PathfindingGrid : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase wallTile;

    private Dictionary<Vector3Int, Node> grid = new Dictionary<Vector3Int, Node>();
    private List<Vector3Int> walkableTiles = new List<Vector3Int>();  

    public Dictionary<Vector3Int, Node> GetGrid() { return grid; }
    public List<Vector3Int> GetWalkableTiles() { return walkableTiles; } 

    public void GenerateGrid()
    {
        BoundsInt bounds = tilemap.cellBounds;

        walkableTiles.Clear();  

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(tilePosition);

                bool isWalkable = tile != wallTile;

                Node node = new Node(tilePosition, isWalkable);
                grid[tilePosition] = node;

                if (isWalkable)
                {
                    walkableTiles.Add(tilePosition);
                }
            }
        }
    }

    public Node GetNode(Vector3Int position)
    {
        grid.TryGetValue(position, out Node node);
        return node;
    }

    public bool IsPathTile(Vector3Int tile)
    {
        return grid.ContainsKey(tile) && grid[tile].isWalkable; 
    }

    public Vector3Int GetRandomPathTile()
    {

        int randomIndex = Random.Range(0, walkableTiles.Count);
        return walkableTiles[randomIndex];
    }
}
