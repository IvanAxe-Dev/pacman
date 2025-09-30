using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerMovement : MonoBehaviour
{
    public Tilemap tilemap;
    public string wallTileName = "Wall";
    public float moveSpeed = 3f; 

    private Vector3Int currentTilePosition;
    private Vector3 targetPosition;
    private Vector3Int movementDirection;
    private bool isMoving = false;

    private void Start()
    {
        currentTilePosition = tilemap.WorldToCell(transform.position);
        transform.position = tilemap.GetCellCenterWorld(currentTilePosition);
        targetPosition = transform.position;
        movementDirection = Vector3Int.zero; 
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) movementDirection = Vector3Int.up;
        else if (Input.GetKeyDown(KeyCode.S)) movementDirection = Vector3Int.down;
        else if (Input.GetKeyDown(KeyCode.A)) movementDirection = Vector3Int.left;
        else if (Input.GetKeyDown(KeyCode.D)) movementDirection = Vector3Int.right;

        if (!isMoving && movementDirection != Vector3Int.zero)
        {
            TryMove(movementDirection);
        }

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                TryMove(movementDirection);
            }
        }
    }

    private void TryMove(Vector3Int direction)
    {
        Vector3Int targetTilePosition = currentTilePosition + direction;

        if (!IsWallTile(targetTilePosition))
        {
            currentTilePosition = targetTilePosition;
            targetPosition = tilemap.GetCellCenterWorld(currentTilePosition);
            isMoving = true;
        }
        else
        {
            isMoving = false;
            movementDirection = Vector3Int.zero;
        }
    }

    private bool IsWallTile(Vector3Int tilePosition)
    {
        TileBase tile = tilemap.GetTile(tilePosition);
        return tile != null && tile.name == wallTileName;
    }
}
