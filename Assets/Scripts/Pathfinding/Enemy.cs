using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Ghost : MonoBehaviour
{
    private PathfindingGrid pathfindingGrid;
    private Transform player;
    public GameObject ghostModel;
    public GameObject spawnEffect;
    public enum GhostType { Blinky, Pinky, Inky, Clyde }
    public GhostType ghostType;
    public enum DifficultyLevel { Easy, Medium, Hard }
    private static DifficultyLevel currentDifficulty = DifficultyLevel.Easy;

    private List<Vector3Int> path;
    private int currentPathIndex = 0;
    public float baseSpeed = 3f;
    private float currentSpeed;
    public float timeForNewPath = 2f;
    private float pathRecalculateTimer = 0f;

    // Vision parameters
    private float visionRange = 5f;
    private bool canSeePlayer = false;
    private bool canSeeOtherGhosts = false;
    private float memoryTime = 3f; // How long ghost remembers player position
    private Vector3 lastKnownPlayerPosition;
    private float memoryCooldown = 0f;

    // Group behavior
    private float separationRadius = 2f; // Keep distance from other ghosts
    private float coordinationRadius = 6f; // Range for ghost coordination

    public int maxHealthPoints = 2;
    public int healthPoints;
    public Image healthBar;

    private void Start()
    {
        healthPoints = maxHealthPoints;
        GameObject myEffect = Instantiate(spawnEffect, transform);
        Destroy(myEffect, 2);

        player = GameManager.Instance.playerTransform;
        pathfindingGrid = GameManager.Instance.pathfindingGrid;

        // Assign random ghost type if not set
        if (Random.Range(0f, 1f) < 0.25f)
            ghostType = (GhostType)Random.Range(0, 4);

        ApplyDifficultySettings();
        StartCoroutine(BehaviorRoutine());
    }

    void ApplyDifficultySettings()
    {
        switch (currentDifficulty)
        {
            case DifficultyLevel.Easy:
                visionRange = 6f; // Slightly increased vision
                canSeeOtherGhosts = false;
                currentSpeed = baseSpeed * 3f;
                memoryTime = 1.5f;
                timeForNewPath = 2.5f;
                break;

            case DifficultyLevel.Medium:
                visionRange = 8f;
                canSeeOtherGhosts = true;
                currentSpeed = baseSpeed * 3f;
                memoryTime = 3f;
                timeForNewPath = 2f;
                break;

            case DifficultyLevel.Hard:
                visionRange = 10f;
                canSeeOtherGhosts = true;
                currentSpeed = baseSpeed * 3f;
                memoryTime = 5f;
                timeForNewPath = 1f; // Recalculates path much faster
                break;
        }
    }

    public static void SetDifficulty(DifficultyLevel level)
    {
        currentDifficulty = level;

        // Update all existing ghosts
        Ghost[] ghosts = FindObjectsOfType<Ghost>();
        foreach (Ghost ghost in ghosts)
        {
            ghost.ApplyDifficultySettings();
        }
    }

    IEnumerator BehaviorRoutine()
    {
        while (true)
        {
            // Check if player is in vision range
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            canSeePlayer = distanceToPlayer <= visionRange && HasLineOfSight(player.position);

            if (canSeePlayer)
            {
                lastKnownPlayerPosition = player.position;
                memoryCooldown = memoryTime;
            }

            // Decide behavior based on ghost type and situation
            ExecuteGhostBehavior();

            yield return new WaitForSeconds(0.2f); // Update behavior 5 times per second
        }
    }

    void ExecuteGhostBehavior()
    {
        Vector3Int targetPosition = Vector3Int.zero;
        bool shouldRecalculate = false;

        // Memory decay
        if (memoryCooldown > 0)
        {
            memoryCooldown -= 0.2f;
        }

        switch (ghostType)
        {
            case GhostType.Blinky: // Red - Direct hunter
                if (canSeePlayer)
                {
                    targetPosition = pathfindingGrid.tilemap.WorldToCell(player.position);
                    shouldRecalculate = true;
                }
                else if (memoryCooldown > 0)
                {
                    targetPosition = pathfindingGrid.tilemap.WorldToCell(lastKnownPlayerPosition);
                    shouldRecalculate = true;
                }
                else
                {
                    // Patrol random areas
                    if (path == null || currentPathIndex >= path.Count)
                    {
                        targetPosition = GetRandomPatrolPoint();
                        shouldRecalculate = true;
                    }
                }
                break;

            case GhostType.Pinky: // Pink - Ambusher (tries to get ahead of player)
                if (canSeePlayer)
                {
                    // Try to predict player movement
                    Vector3 predictedPos = PredictPlayerPosition(4);
                    targetPosition = pathfindingGrid.tilemap.WorldToCell(predictedPos);
                    shouldRecalculate = true;
                }
                else
                {
                    targetPosition = GetStrategicPatrolPoint();
                    shouldRecalculate = pathRecalculateTimer <= 0;
                }
                break;

            case GhostType.Inky: // Cyan - Flanker
                if (canSeeOtherGhosts && currentDifficulty != DifficultyLevel.Easy)
                {
                    // Try to flank with other ghosts
                    targetPosition = GetFlankingPosition();
                    shouldRecalculate = true;
                }
                else if (canSeePlayer)
                {
                    // Keep medium distance
                    Vector3 dirToPlayer = (player.position - transform.position).normalized;
                    Vector3 mediumPos = player.position - dirToPlayer * 3f;
                    targetPosition = pathfindingGrid.tilemap.WorldToCell(mediumPos);
                    shouldRecalculate = true;
                }
                else
                {
                    targetPosition = GetRandomPatrolPoint();
                    shouldRecalculate = pathRecalculateTimer <= 0;
                }
                break;

            case GhostType.Clyde: // Orange - Shy/Random
                float distToPlayer = Vector3.Distance(transform.position, player.position);
                if (distToPlayer < 4f && canSeePlayer)
                {
                    // Run away if too close
                    Vector3 awayDir = (transform.position - player.position).normalized;
                    Vector3 escapePos = transform.position + awayDir * 5f;
                    targetPosition = pathfindingGrid.tilemap.WorldToCell(escapePos);
                    shouldRecalculate = true;
                }
                else if (distToPlayer > 8f && canSeePlayer)
                {
                    // Chase if far enough
                    targetPosition = pathfindingGrid.tilemap.WorldToCell(player.position);
                    shouldRecalculate = true;
                }
                else
                {
                    // Random patrol
                    if (path == null || currentPathIndex >= path.Count)
                    {
                        targetPosition = GetRandomPatrolPoint();
                        shouldRecalculate = true;
                    }
                }
                break;
        }

        if (shouldRecalculate && targetPosition != Vector3Int.zero)
        {
            CalculatePath(targetPosition);
            pathRecalculateTimer = timeForNewPath;
        }

        // Apply separation from other ghosts if enabled
        if (canSeeOtherGhosts && currentDifficulty != DifficultyLevel.Easy)
        {
            ApplySeparation();
        }
    }

    Vector3 PredictPlayerPosition(int tilesAhead)
    {
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            // Simple prediction based on player's current direction
            Vector3 playerVelocity = player.GetComponent<Rigidbody2D>()?.velocity ?? Vector3.zero;
            return player.position + playerVelocity.normalized * tilesAhead;
        }
        return player.position;
    }

    Vector3Int GetFlankingPosition()
    {
        // Find other ghosts and try to surround player
        Ghost[] otherGhosts = FindObjectsOfType<Ghost>();
        Vector3 flankPos = player.position;

        foreach (Ghost other in otherGhosts)
        {
            if (other != this && Vector3.Distance(other.transform.position, player.position) < coordinationRadius)
            {
                // Calculate position opposite to other ghost
                Vector3 dirFromOther = (player.position - other.transform.position).normalized;
                flankPos = player.position + dirFromOther * 3f;
                break;
            }
        }

        return pathfindingGrid.tilemap.WorldToCell(flankPos);
    }

    Vector3Int GetStrategicPatrolPoint()
    {
        // Patrol intersections and key points
        List<Vector3Int> strategicPoints = new List<Vector3Int>();

        // Find intersections (tiles with 3+ walkable neighbors)
        foreach (var tile in pathfindingGrid.GetWalkableTiles())
        {
            int walkableNeighbors = 0;
            Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

            foreach (var dir in directions)
            {
                if (pathfindingGrid.IsPathTile(tile + dir))
                    walkableNeighbors++;
            }

            if (walkableNeighbors >= 3)
                strategicPoints.Add(tile);
        }

        if (strategicPoints.Count > 0)
            return strategicPoints[Random.Range(0, strategicPoints.Count)];

        return GetRandomPatrolPoint();
    }

    Vector3Int GetRandomPatrolPoint()
    {
        return pathfindingGrid.GetRandomPathTile();
    }

    bool HasLineOfSight(Vector3 target)
    {
        // Simple line of sight check in maze
        Vector3 direction = target - transform.position;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, visionRange, LayerMask.GetMask("Wall"));

        return hit.collider == null || hit.distance > Vector3.Distance(transform.position, target);
    }

    void ApplySeparation()
    {
        Ghost[] ghosts = FindObjectsOfType<Ghost>();
        Vector3 separationVector = Vector3.zero;

        foreach (Ghost other in ghosts)
        {
            if (other != this)
            {
                float dist = Vector3.Distance(transform.position, other.transform.position);
                if (dist < separationRadius && dist > 0)
                {
                    Vector3 awayDir = (transform.position - other.transform.position).normalized;
                    separationVector += awayDir * (1f - dist / separationRadius);
                }
            }
        }

        if (separationVector.magnitude > 0)
        {
            transform.position += separationVector.normalized * currentSpeed * Time.deltaTime * 0.3f;
        }
    }

    void CalculatePath(Vector3Int targetPosition)
    {
        Vector3Int start = pathfindingGrid.tilemap.WorldToCell(transform.position);

        Node startNode = pathfindingGrid.GetNode(start);
        Node targetNode = pathfindingGrid.GetNode(targetPosition);

        if (startNode != null && targetNode != null)
        {
            path = AStarPathfinding.FindPath(startNode, targetNode, pathfindingGrid.GetGrid());
            currentPathIndex = 0;
        }
    }

    private void Update()
    {
        pathRecalculateTimer -= Time.deltaTime;

        if (path != null && currentPathIndex < path.Count)
        {
            Vector3 targetWorldPosition = pathfindingGrid.tilemap.GetCellCenterWorld(path[currentPathIndex]);
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, currentSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetWorldPosition) < 0.1f)
            {
                currentPathIndex++;
            }

            // Rotate model towards movement direction
            if (ghostModel != null && path.Count > currentPathIndex)
            {
                Vector3 moveDir = (pathfindingGrid.tilemap.GetCellCenterWorld(path[currentPathIndex]) - transform.position).normalized;
                if (moveDir != Vector3.zero)
                {
                    ghostModel.transform.rotation = Quaternion.LookRotation(Vector3.forward, moveDir);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<Player>().GetDamage();
        }

        if (collision.CompareTag("Bullet"))
        {
            GetDamage();
            Destroy(collision.gameObject);
        }
    }

    public void GetDamage()
    {
        healthPoints--;
        if (healthBar != null)
            healthBar.fillAmount = (float)healthPoints / maxHealthPoints;

        if (healthPoints <= 0)
            Die();
    }

    public void Die()
    {
        GameObject myEffect = Instantiate(spawnEffect, transform.position, transform.rotation);
        Destroy(myEffect, 2);
        GameManager.Instance.OnGhostDeath();
        Destroy(gameObject);
    }
}