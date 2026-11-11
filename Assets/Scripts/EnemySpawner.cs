using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    public GameObject zombiePrefab;
    public GameObject enemyBPrefab;
    public int initialEnemyCount = 3;
    public int maxEnemyCount = 7;
    public float minSpawnDistance = 15f;
    public float arenaRadius = 22f;
    public float spawnCheckInterval = 2.5f;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private Transform playerTransform;
    private float nextCheckTime;
    private bool initialSpawnDone;
    private bool spawnFailed;
    private Coroutine initialSpawnRoutine;

    void Start()
    {
        FindPlayer();
        if (zombiePrefab == null) zombiePrefab = Resources.Load<GameObject>("Zombie");
        if (enemyBPrefab == null) enemyBPrefab = Resources.Load<GameObject>("EnemyB");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStarted += OnGameStarted;
            GameManager.Instance.OnGameOver += OnGameOver;

            // A UI pode iniciar a partida antes deste Start() rodar.
            if (GameManager.Instance.IsPlaying && !initialSpawnDone)
            {
                OnGameStarted();
            }
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStarted -= OnGameStarted;
            GameManager.Instance.OnGameOver -= OnGameOver;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || !initialSpawnDone) return;
        if (Time.time < nextCheckTime) return;

        nextCheckTime = Time.time + spawnCheckInterval;
        CleanupDestroyedEnemies();
        int score = GameManager.Instance.Score;
        int desiredCount = Mathf.Min(initialEnemyCount + score / 30, maxEnemyCount);
        if (activeEnemies.Count < desiredCount && !SpawnSingleEnemy())
        {
            if (!spawnFailed)
            {
                Debug.LogWarning("[EnemySpawner] Não foi possível encontrar uma área livre na NavMesh para outro inimigo.");
                spawnFailed = true;
            }
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null) playerTransform = playerObject.transform;
        else if (Camera.main != null) playerTransform = Camera.main.transform.parent != null ? Camera.main.transform.parent : Camera.main.transform;
    }

    private void OnGameStarted()
    {
        CleanupAllEnemies();
        if (playerTransform == null) FindPlayer();
        initialSpawnDone = false;
        spawnFailed = false;
        if (initialSpawnRoutine != null) StopCoroutine(initialSpawnRoutine);
        initialSpawnRoutine = StartCoroutine(SpawnInitialWave());
    }

    private IEnumerator SpawnInitialWave()
    {
        // Pequeno período de respiro antes da primeira horda.
        yield return new WaitForSeconds(2.5f);

        int spawned = 0;
        for (int i = 0; i < initialEnemyCount; i++)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) yield break;
            if (SpawnSingleEnemy()) spawned++;
            yield return new WaitForSeconds(0.7f);
        }

        if (spawned == 0) Debug.LogError("[EnemySpawner] A NavMesh não ofereceu pontos válidos de spawn.");
        nextCheckTime = Time.time + spawnCheckInterval;
        initialSpawnDone = true;
        initialSpawnRoutine = null;
    }

    private void OnGameOver()
    {
        initialSpawnDone = false;
        if (initialSpawnRoutine != null)
        {
            StopCoroutine(initialSpawnRoutine);
            initialSpawnRoutine = null;
        }
        ZombieController[] enemies = Object.FindObjectsByType<ZombieController>();
        foreach (ZombieController enemy in enemies)
        {
            if (enemy == null) continue;
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
        }
    }

    private void CleanupDestroyedEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null) activeEnemies.RemoveAt(i);
        }
    }

    private void CleanupAllEnemies()
    {
        ZombieController[] enemies = Object.FindObjectsByType<ZombieController>();
        foreach (ZombieController enemy in enemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();
    }

    public bool SpawnSingleEnemy()
    {
        if (playerTransform == null) FindPlayer();
        if (playerTransform == null) return false;

        GameObject prefab = ChoosePrefabToSpawn();
        if (prefab == null || !TryGetSpawnPosition(out Vector3 position)) return false;

        GameObject enemy = Instantiate(prefab, position, Quaternion.identity);
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null && !agent.isOnNavMesh && NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas)) agent.Warp(hit.position);
        activeEnemies.Add(enemy);
        spawnFailed = false;
        return true;
    }

    private GameObject ChoosePrefabToSpawn()
    {
        if (zombiePrefab == null) return enemyBPrefab;
        if (enemyBPrefab == null) return zombiePrefab;
        int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        float enemyBChance = score >= 50 ? 0.65f : 0.35f;
        return Random.value < enemyBChance ? enemyBPrefab : zombiePrefab;
    }

    private bool TryGetSpawnPosition(out Vector3 result)
    {
        result = Vector3.zero;
        Vector3 center = playerTransform.position;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector2 direction = Random.insideUnitCircle;
            if (direction.sqrMagnitude < 0.01f) continue;
            direction.Normalize();
            float radius = Random.Range(minSpawnDistance, arenaRadius);
            Vector3 candidate = center + new Vector3(direction.x * radius, 0f, direction.y * radius);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas)) continue;

            Vector3 horizontal = hit.position - center;
            horizontal.y = 0f;
            if (horizontal.magnitude < minSpawnDistance) continue;
            if (Physics.CheckSphere(hit.position + Vector3.up * 0.9f, 0.55f, ~0, QueryTriggerInteraction.Ignore)) continue;

            result = hit.position;
            return true;
        }
        return false;
    }

    public void NotifyEnemyDied(GameObject enemy)
    {
        activeEnemies.Remove(enemy);
    }

    public int ActiveEnemyCount
    {
        get
        {
            CleanupDestroyedEnemies();
            return activeEnemies.Count;
        }
    }
}
