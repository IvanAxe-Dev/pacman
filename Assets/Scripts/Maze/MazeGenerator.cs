using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MazeGenerator : MonoBehaviour
{
    public int width = 21;
    public int height = 21;
    public Tilemap tilemap; 
    public TileBase wallTile; 
    public TileBase pathTile;
    public TileBase portalTile;

    public void GenerateAndRenderMaze()
    {
        int[,] maze = GenerateMaze();

        int[,] expandedMaze = ExpandMaze(maze);
        expandedMaze = AddCentralRectangle(expandedMaze, 6, 6, 2);

        RenderMaze(expandedMaze);
    }

    public int[,] GenerateMaze()
    {
        int[,] maze = new int[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                maze[y, x] = 1;
            }
        }

        int startX = Random.Range(1, width - 1);
        int startY = Random.Range(1, height - 1);
        startX = startX % 2 == 0 ? startX + 1 : startX;
        startY = startY % 2 == 0 ? startY + 1 : startY;

        maze[startY, startX] = 0;

        List<Vector2Int> frontiers = new List<Vector2Int>();
        AddFrontiers(startX, startY, frontiers, maze);

        while (frontiers.Count > 0)
        {
            int randomIndex = Random.Range(0, frontiers.Count);
            Vector2Int frontier = frontiers[randomIndex];
            frontiers.RemoveAt(randomIndex);

            int x = frontier.x;
            int y = frontier.y;

            List<Vector2Int> neighbors = GetNeighbors(x, y, maze);
            if (neighbors.Count > 0)
            {
                Vector2Int connectedNeighbor = neighbors[Random.Range(0, neighbors.Count)];
                int nx = connectedNeighbor.x;
                int ny = connectedNeighbor.y;

                maze[y, x] = 0;
                maze[(y + ny) / 2, (x + nx) / 2] = 0;

                AddFrontiers(x, y, frontiers, maze);
            }
        }
        return maze;
    }



    private void AddFrontiers(int x, int y, List<Vector2Int> frontiers, int[,] maze)
    {
        foreach (Vector2Int dir in new Vector2Int[] {
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(0, 2),
            new Vector2Int(0, -2)
        })
        {
            int fx = x + dir.x;
            int fy = y + dir.y;
            if (fx > 0 && fy > 0 && fx < maze.GetLength(1) - 1 && fy < maze.GetLength(0) - 1 && maze[fy, fx] == 1)
            {
                frontiers.Add(new Vector2Int(fx, fy));
            }
        }
    }

    private List<Vector2Int> GetNeighbors(int x, int y, int[,] maze)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        foreach (Vector2Int dir in new Vector2Int[] {
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(0, 2),
            new Vector2Int(0, -2)
        })
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            if (nx > 0 && ny > 0 && nx < maze.GetLength(1) && ny < maze.GetLength(0) && maze[ny, nx] == 0)
            {
                neighbors.Add(new Vector2Int(nx, ny));
            }
        }

        return neighbors;
    }

    private void RenderMaze(int[,] maze)
    {
        tilemap.ClearAllTiles();

        for (int y = 0; y < maze.GetLength(0); y++)
        {
            for (int x = 0; x < maze.GetLength(1); x++)
            {
                if (maze[y, x] == 1)
                {
                    tilemap.SetTile(new Vector3Int(x, -y, 0), wallTile);
                }
                else if (maze[y, x] == 0)
                {
                    tilemap.SetTile(new Vector3Int(x, -y, 0), pathTile);
                } else if (maze[y, x] == 2)
                {
                    tilemap.SetTile(new Vector3Int(x, -y, 0), portalTile);
                }
            }
        }
    }

    int[,] ExpandMaze(int[,] maze)
    {
        int originalRows = maze.GetLength(0);
        int originalCols = maze.GetLength(1);

        int newRows = originalRows * 2;
        int newCols = originalCols * 2;

        int[,] expandedMaze = new int[newRows, newCols];

        for (int i = 0; i < originalRows; i++)
        {
            for (int j = 0; j < originalCols; j++)
            {
                int value = maze[i, j];
                int newRow = i * 2;
                int newCol = j * 2;

                expandedMaze[newRow, newCol] = value;       
                expandedMaze[newRow, newCol + 1] = value;    
                expandedMaze[newRow + 1, newCol] = value;    
                expandedMaze[newRow + 1, newCol + 1] = value; 
            }
        }

        return expandedMaze;
    }

    public int[,] AddCentralRectangle(int[,] maze, int width, int height, int portalTile)
    {
        int mazeHeight = maze.GetLength(0);
        int mazeWidth = maze.GetLength(1);

        int centerX = mazeWidth / 2;
        int centerY = mazeHeight / 2;

        int startX = centerX - width / 2;
        int startY = centerY - height / 2;

        startX = Mathf.Max(0, startX);
        startY = Mathf.Max(0, startY);
        int endX = Mathf.Min(mazeWidth, startX + width);
        int endY = Mathf.Min(mazeHeight, startY + height);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                maze[y, x] = 0;  
            }
        }

        int portalStartX = centerX - 1;
        int portalStartY = centerY - 1;


        if (portalStartX >= 0 && portalStartX + 2 <= mazeWidth && portalStartY >= 0 && portalStartY + 2 <= mazeHeight)
        {
            for (int y = portalStartY; y < portalStartY + 2; y++)
            {
                for (int x = portalStartX; x < portalStartX + 2; x++)
                {
                    maze[y, x] = portalTile;  
                }
            }
        }

        return maze;
    }





}
