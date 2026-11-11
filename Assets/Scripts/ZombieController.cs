using UnityEngine;
using UnityEngine.AI;

public class ZombieController : MonoBehaviour
{
    public float speed = 2.4f;
    public int points = 10;
    public float contactRadius = 0.85f;
    public float stoppingDistance = 0.6f;

    public string walkAnimName = "walk";
    public string attackAnimName = "attack";
    public string deathAnimName = "back_fall";

    public AudioClip groanSound;
    public AudioClip attackSound;
    public AudioClip deathSound;

    private Transform playerTarget;
    private NavMeshAgent agent;
    private Animation legacyAnim;
    private Animator modernAnimator;
    private AudioSource audioSource;
    private Collider[] enemyColliders;
    private bool isDead;
    private float nextDestinationUpdateTime;
    private float nextGroanTime;
    private float attackCooldownUntil;
    private float spawnTime;

    public bool IsDead => isDead;
    public AudioSource VoiceSource => audioSource;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        legacyAnim = GetComponent<Animation>();
        modernAnimator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        enemyColliders = GetComponentsInChildren<Collider>(true);
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        ConfigureVoiceSource();
    }

    void OnEnable()
    {
        PlayerWaypointMover.OnPlayerMoved += HandlePlayerMoved;
    }

    void OnDisable()
    {
        PlayerWaypointMover.OnPlayerMoved -= HandlePlayerMoved;
    }

    void Start()
    {
        spawnTime = Time.time;
        FindPlayerTarget();

        if (agent != null)
        {
            agent.speed = speed;
            agent.stoppingDistance = stoppingDistance;
            agent.autoRepath = true;
        }

        UpdateDestination();
        PlayAnim(walkAnimName);
        ScheduleNextGroan();
    }

    void Update()
    {
        if (isDead) return;

        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            if (agent != null && agent.isActiveAndEnabled && !agent.isStopped) agent.isStopped = true;
            return;
        }

        if (Time.time < attackCooldownUntil) return;
        if (agent != null && agent.isActiveAndEnabled && agent.isStopped) agent.isStopped = false;

        if (playerTarget == null) FindPlayerTarget();
        if (playerTarget == null) return;

        if (Time.time >= nextDestinationUpdateTime)
        {
            nextDestinationUpdateTime = Time.time + 0.25f;
            UpdateDestination();
        }

        if (Time.time >= nextGroanTime)
        {
            PlayGroanSound();
            ScheduleNextGroan();
        }
    }

    private void FindPlayerTarget()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null) playerTarget = playerObject.transform;
        else if (Camera.main != null) playerTarget = Camera.main.transform.parent != null ? Camera.main.transform.parent : Camera.main.transform;
    }

    public void UpdateDestination()
    {
        if (isDead || agent == null || !agent.isActiveAndEnabled || playerTarget == null || !agent.isOnNavMesh) return;
        if (!NavMesh.SamplePosition(playerTarget.position, out NavMeshHit hit, 3f, agent.areaMask)) return;
        agent.SetDestination(hit.position);
    }

    private void HandlePlayerMoved(Vector3 position)
    {
        if (playerTarget == null) FindPlayerTarget();
        if (playerTarget != null) UpdateDestination();
    }

    public void AttackPlayer(Vector3 playerPosition)
    {
        if (isDead || (GameManager.Instance != null && !GameManager.Instance.IsPlaying)) return;
        attackCooldownUntil = Time.time + 1.25f;
        if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;
        PlayAnim(attackAnimName);
        if (audioSource != null && attackSound != null)
        {
            audioSource.pitch = Random.Range(0.96f, 1.04f);
            audioSource.PlayOneShot(attackSound);
        }
    }

    public void Knockback(Vector3 fromPosition)
    {
        attackCooldownUntil = Mathf.Max(attackCooldownUntil, Time.time + 0.8f);
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        Vector3 pushDirection = transform.position - fromPosition;
        pushDirection.y = 0f;
        if (pushDirection.sqrMagnitude < 0.01f) pushDirection = transform.forward;
        Vector3 destination = transform.position + pushDirection.normalized * 0.85f;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 1.5f, agent.areaMask)) agent.Warp(hit.position);
        agent.isStopped = true;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (isDead || other == null) return;

        BulletProjectile bullet = other.GetComponent<BulletProjectile>() ?? other.GetComponentInParent<BulletProjectile>();
        if (bullet != null) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        for (int i = 0; i < enemyColliders.Length; i++)
        {
            if (enemyColliders[i] != null) enemyColliders[i].enabled = false;
        }

        if (agent != null && agent.isActiveAndEnabled)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
        }

        if (audioSource != null) audioSource.Stop();
        PlayAnim(deathAnimName);
        if (audioSource != null && deathSound != null)
        {
            audioSource.pitch = Random.Range(0.96f, 1.04f);
            audioSource.PlayOneShot(deathSound);
        }

        if (GameManager.Instance != null) GameManager.Instance.AddScore(points);
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner != null) spawner.NotifyEnemyDied(gameObject);
        Destroy(gameObject, 4f);
    }

    private void PlayAnim(string animationName)
    {
        if (string.IsNullOrEmpty(animationName)) return;

        if (legacyAnim != null && legacyAnim.GetClip(animationName) != null)
        {
            legacyAnim.CrossFade(animationName, 0.1f);
        }
        else if (modernAnimator != null && modernAnimator.runtimeAnimatorController != null)
        {
            modernAnimator.CrossFade(animationName, 0.1f, 0);
        }
    }

    private void ConfigureVoiceSource()
    {
        if (audioSource == null) return;
        AudioManager.ConfigureAudioSource3D(audioSource, 2f, 30f);
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void PlayGroanSound()
    {
        if (audioSource == null || groanSound == null || isDead) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(groanSound, 0.55f);
    }

    private void ScheduleNextGroan()
    {
        nextGroanTime = Time.time + Random.Range(3f, 8f);
    }
}
