using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] enemyPrefabs;  
    public float spawnRadius = 8f;
    public int startEnemyCount = 5;
    public int waveIncrease = 2;
    public float spawnDelay = 1f;
    public HUDController hud;

    private Transform player;
    private int currentWave = 0;
    private List<GameObject> aliveEnemies = new List<GameObject>();
    private bool spawningWave = false;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        StartCoroutine(StartNextWave());
    }

    void Update()
    {
        aliveEnemies.RemoveAll(e => e == null);

        if (!spawningWave && aliveEnemies.Count == 0)
        {
            StartCoroutine(StartNextWave());
        }
    }

    IEnumerator StartNextWave()
    {
        spawningWave = true;
        currentWave++;

        
        HUDController hud = FindObjectOfType<HUDController>();
        if (hud != null)
            hud.UpdateWave(currentWave);

        int enemyCount = startEnemyCount + (currentWave - 1) * waveIncrease;

        Debug.Log($"Wave {currentWave} - spawning {enemyCount} enemies...");

        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnDelay);
        }

        spawningWave = false;
    }


    void SpawnEnemy()
    {
        if (player == null || enemyPrefabs.Length == 0) return;

        Vector2 spawnDir = Random.insideUnitCircle.normalized;
        Vector2 spawnPos = (Vector2)player.position + spawnDir * spawnRadius;

        
        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject enemy = Instantiate(enemyPrefabs[randomIndex], spawnPos, Quaternion.identity);
        aliveEnemies.Add(enemy);
    }
}
