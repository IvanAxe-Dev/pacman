using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public PathfindingGrid pathfindingGrid;
    public Transform playerTransform;

    [SerializeField] private MazeGenerator m_Generator;
    [SerializeField] private PathfindingGrid m_Grid;
    [SerializeField] private GameObject ghostPrefab;
    [Header("Spawning Settings")]
    [SerializeField] private float minSpawnDistanceFromPlayer = 10f;

    public Text deadGhostsText;
    public Text difficultyText;
    public int deadGhostsCount;
    private int currentGhostCount;
    public int maxGhosts = 4;
    public float respawnDelay = 5f;

    public Animator menuAnimator;
    public Animator gameUIanimator;
    private bool isGameStarted = false;

    public Text lastScoreText, bestScoreText;
    private int lastScore;
    private int bestScore;

    // Difficulty management
    private Ghost.DifficultyLevel currentDifficulty = Ghost.DifficultyLevel.Easy;
    private float difficultyTimer = 0f;
    private float timeToIncreaseDifficulty = 30f;

    private void Awake()
    {
        Instance = this;
        m_Generator.GenerateAndRenderMaze();
        m_Grid.GenerateGrid();
    }

    private void Start()
    {
        // Load scores
        bestScore = PlayerPrefs.HasKey("BestScore") ? PlayerPrefs.GetInt("BestScore") : 0;
        lastScore = PlayerPrefs.HasKey("LastScore") ? PlayerPrefs.GetInt("LastScore") : 0;

        // Update UI with null checks
        if (bestScoreText != null)
            bestScoreText.text = "Best Score: " + bestScore.ToString("D8");

        if (lastScoreText != null)
            lastScoreText.text = "Last Score: " + lastScore.ToString("D8");

        SetDifficulty(Ghost.DifficultyLevel.Easy);
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (deadGhostsText != null)
        {
            deadGhostsText.text = deadGhostsCount.ToString();
        }

        HandleDifficultyInput();

        if (!isGameStarted)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartGame();
            }
        }
        else
        {
            difficultyTimer += Time.deltaTime;
            if (difficultyTimer >= timeToIncreaseDifficulty)
            {
                IncreaseDifficulty();
                difficultyTimer = 0f;
            }
        }
    }

    private void HandleDifficultyInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentDifficulty < Ghost.DifficultyLevel.Hard)
            {
                SetDifficulty(currentDifficulty + 1);
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentDifficulty > Ghost.DifficultyLevel.Easy)
            {
                SetDifficulty(currentDifficulty - 1);
            }
        }
    }

    void SetDifficulty(Ghost.DifficultyLevel level)
    {
        currentDifficulty = level;
        Ghost.SetDifficulty(level);
        UpdateDifficultyDisplay();

        switch (level)
        {
            case Ghost.DifficultyLevel.Easy:
                maxGhosts = 7;
                respawnDelay = 5f;
                break;
            case Ghost.DifficultyLevel.Medium:
                maxGhosts = 7;
                respawnDelay = 5f;
                break;
            case Ghost.DifficultyLevel.Hard:
                maxGhosts = 7;
                respawnDelay = 3f;
                break;
        }
    }

    void IncreaseDifficulty()
    {
        if (currentDifficulty < Ghost.DifficultyLevel.Hard)
        {
            SetDifficulty(currentDifficulty + 1);
        }
    }

    void UpdateDifficultyDisplay()
    {
        if (difficultyText != null)
        {
            string diffText = "Difficulty: ";
            switch (currentDifficulty)
            {
                case Ghost.DifficultyLevel.Easy:
                    diffText += "<color=green>EASY</color>";
                    break;
                case Ghost.DifficultyLevel.Medium:
                    diffText += "<color=yellow>MEDIUM</color>";
                    break;
                case Ghost.DifficultyLevel.Hard:
                    diffText += "<color=red>HARD</color>";
                    break;
            }
            difficultyText.text = diffText + " (Use Up/Down Arrows)";
        }
    }

    private void StartGame()
    {
        isGameStarted = true;
        Time.timeScale = 1f;

        if (menuAnimator != null)
            menuAnimator.SetTrigger("StartTheGame");

        if (Camera.main != null)
        {
            Animator camAnimator = Camera.main.GetComponent<Animator>();
            if (camAnimator != null)
                camAnimator.SetTrigger("StartTheGame");
        }

        if (gameUIanimator != null)
            gameUIanimator.SetTrigger("StartTheGame");

        StartCoroutine(SpawnGhostsOverTime());
    }

    public void EndGame()
    {
        isGameStarted = false;
        Time.timeScale = 0f;

        if (Camera.main != null)
        {
            Animator camAnimator = Camera.main.GetComponent<Animator>();
            if (camAnimator != null)
                camAnimator.SetTrigger("EndTheGame");
        }

        if (playerTransform != null)
        {
            Player player = playerTransform.GetComponent<Player>();
            if (player != null)
            {
                lastScore = player.score;
                if (lastScore > bestScore)
                {
                    bestScore = lastScore;
                    PlayerPrefs.SetInt("BestScore", bestScore);
                }
                PlayerPrefs.SetInt("LastScore", lastScore);
            }
        }

        SceneManager.LoadScene(0);
    }

    private IEnumerator SpawnGhostsOverTime()
    {
        yield return new WaitForSeconds(2f);

        int initialSpawnCount = Mathf.Min(maxGhosts, 4);
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnGhost();
            yield return new WaitForSeconds(1.5f);
        }
    }

    void SpawnGhost()
    {
        if (ghostPrefab == null)
        {
            Debug.LogError("Ghost Prefab has not been assigned in the GameManager Inspector!");
            return;
        }

        if (currentGhostCount < maxGhosts)
        {
            Vector3 spawnWorldPosition = Vector3.zero;
            bool safeSpotFound = false;
            int attempts = 0;
            const int maxAttempts = 50;

            while (!safeSpotFound && attempts < maxAttempts)
            {
                attempts++;
                Vector3Int potentialTile = pathfindingGrid.GetRandomPathTile();
                Vector3 potentialWorldPos = pathfindingGrid.tilemap.GetCellCenterWorld(potentialTile);

                // Calculate the distance from the potential spawn point to the player
                float distanceToPlayer = Vector3.Distance(potentialWorldPos, playerTransform.position);

                // If the distance is greater than our minimum, found a safe spot
                if (distanceToPlayer >= minSpawnDistanceFromPlayer)
                {
                    spawnWorldPosition = potentialWorldPos;
                    safeSpotFound = true;
                }
            }

            // just spawn at the last random spot we found to avoid errors.
            if (!safeSpotFound)
            {
                Debug.LogWarning("Could not find a spawn point far enough from the player after " + maxAttempts + " attempts. Spawning at a random location anyway.");
                // If we never found a safe spot, spawnWorldPosition will be zero. Let's get one last random position.
                if (spawnWorldPosition == Vector3.zero)
                {
                    spawnWorldPosition = pathfindingGrid.tilemap.GetCellCenterWorld(pathfindingGrid.GetRandomPathTile());
                }
            }

            Instantiate(ghostPrefab, spawnWorldPosition, Quaternion.identity);
            currentGhostCount++;
        }
    }

    public void OnGhostDeath()
    {
        deadGhostsCount++;
        currentGhostCount--;

        if (playerTransform != null)
        {
            Player player = playerTransform.GetComponent<Player>();
            if (player != null)
            {
                player.AddScore(200);
            }
        }

        StartCoroutine(RespawnGhostAfterDelay());
    }

    private IEnumerator RespawnGhostAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (isGameStarted && currentGhostCount < maxGhosts)
        {
            SpawnGhost();
        }
    }
}