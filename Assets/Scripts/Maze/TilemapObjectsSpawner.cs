using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapSpawner : MonoBehaviour
{
    public static TilemapSpawner Instance;
    public Tilemap tilemap; 
    public TileBase[] noSpawnTiles;
    public GameObject spawnPrefab; 
    [Range(0, 100)]
    public float fillPercentage = 50f; 

    void Start()
    {
        Instance = this;
        SpawnOnValidTiles();
    }

    public void SpawnOnValidTiles()
    {
        if (tilemap == null || noSpawnTiles == null || spawnPrefab == null)
        {
            Debug.LogError("Tilemap, No Spawn Tiles, or Spawn Prefab is not assigned!");
            return;
        }

        BoundsInt bounds = tilemap.cellBounds;

        var validTilePositions = new System.Collections.Generic.List<Vector3Int>();

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);

                TileBase tile = tilemap.GetTile(tilePosition);

                if (!IsNoSpawnTile(tile))
                {
                    validTilePositions.Add(tilePosition);
                }
            }
        }

        ShuffleList(validTilePositions);

        int tilesToSpawn = Mathf.RoundToInt(validTilePositions.Count * (fillPercentage / 100f));
        GameManager.Instance.playerTransform.GetComponent<Player>().dotsToWin = tilesToSpawn;

        for (int i = 0; i < tilesToSpawn; i++)
        {
            Vector3Int tilePosition = validTilePositions[i];
            Vector3 worldPosition = tilemap.GetCellCenterWorld(tilePosition);
            Instantiate(spawnPrefab, worldPosition, Quaternion.identity);
        }
    }

    bool IsNoSpawnTile(TileBase tile)
    {
        foreach (TileBase noSpawnTile in noSpawnTiles)
        {
            if (tile == noSpawnTile)
            {
                return true;
            }
        }
        return false;
    }

    void ShuffleList(System.Collections.Generic.List<Vector3Int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Vector3Int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
