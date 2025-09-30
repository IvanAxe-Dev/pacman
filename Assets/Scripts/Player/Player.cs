using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public int score;
    public Text scoreText;
    public GameObject playerModel;
    public Texture2D crosshair;

    public GameObject projectilePrefab;
    public Transform shootPoint;      
    public float shootCooldown = 1f;  

    private float lastShootTime = -1f;

    public Sprite emptyHeart, fullHeart;
    public Image[] hearts = new Image[3];
    public int maxHealthPoints = 3;
    public int healthPoints;

    public float damageCooldown = 0.5f;
    private float cooldownTimer;
    public bool gotDamageRecently = false;

    public int dotsToWin;
    private int dotsEated;

    private void Start()
    {
        healthPoints = maxHealthPoints;
        cooldownTimer = damageCooldown;
        UpdateScoreText();
        Cursor.SetCursor(crosshair, Vector2.zero, CursorMode.Auto);
    }

    private void Update()
    {
        RotatePlayerModelTowardsMouse();
        HandleShooting();

        if(gotDamageRecently)
        {
            if(cooldownTimer >= 0)
                cooldownTimer -= Time.deltaTime;
            else{
                cooldownTimer = damageCooldown;
                gotDamageRecently = false;
                GetComponent<Animator>().SetBool("gotDamageRecently", false);
            }
        }
    }

    private void RotatePlayerModelTowardsMouse()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = new Vector2(
            mousePosition.x - playerModel.transform.position.x,
            mousePosition.y - playerModel.transform.position.y
        );
        playerModel.transform.up = direction;
    }

    private void HandleShooting()
    {
        if (Input.GetMouseButton(0) && Time.time >= lastShootTime + shootCooldown)
        {
            Shoot();
            lastShootTime = Time.time; 
        }
    }

    private void Shoot()
    {
        Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Point"))
        {
            AddScore(25);
            Destroy(collision.gameObject);
            dotsEated++;
            if(dotsEated >= dotsToWin)
            {
                TilemapSpawner.Instance.SpawnOnValidTiles();
            }
        }
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        scoreText.text = score.ToString("D8");
    }

    public void GetDamage()
    {
        if (!gotDamageRecently)
        {
            GetComponent<Animator>().SetBool("gotDamageRecently", true);
            healthPoints--;
            gotDamageRecently = true;
            UpdateHearts();
        }

        if(healthPoints <= 0)
        {
            GameManager.Instance.EndGame();
        }
    }

    public void UpdateHearts()
    {
        for (int i = maxHealthPoints - 1; i >= healthPoints; i--)
        {
            hearts[i].sprite = emptyHeart;
        }
    }


}
