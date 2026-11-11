using UnityEngine;

public class PlayerHitDetector : MonoBehaviour
{
    public float contactRadius = 0.7f;
    public float damageCooldown = 2.0f;

    private float nextDamageTime;
    private AudioSource audioSource;
    private AudioClip hurtSound;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        hurtSound = Resources.Load<AudioClip>("Sons/zombie_attack");
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || Time.time < nextDamageTime) return;

        ZombieController[] enemies = Object.FindObjectsByType<ZombieController>();
        foreach (ZombieController enemy in enemies)
        {
            if (enemy == null || enemy.IsDead || Mathf.Abs(enemy.transform.position.y - transform.position.y) > 1.8f) continue;

            Vector3 difference = enemy.transform.position - transform.position;
            difference.y = 0f;
            if (difference.sqrMagnitude <= contactRadius * contactRadius && HasClearContact(enemy))
            {
                ReceiveDamage(enemy);
                return;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || Time.time < nextDamageTime) return;
        if (other.GetComponent<BulletProjectile>() != null || other.GetComponentInParent<WaypointController>() != null) return;

        ZombieController enemy = other.GetComponent<ZombieController>() ?? other.GetComponentInParent<ZombieController>();
        if (enemy != null && !enemy.IsDead) ReceiveDamage(enemy);
    }

    private bool HasClearContact(ZombieController enemy)
    {
        Vector3 from = transform.position + Vector3.up;
        Vector3 direction = (enemy.transform.position + Vector3.up) - from;
        float distance = direction.magnitude;
        if (distance < 0.01f) return true;

        RaycastHit[] hits = Physics.RaycastAll(from, direction / distance, distance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            return hit.transform == enemy.transform || hit.transform.IsChildOf(enemy.transform);
        }
        return true;
    }

    private void ReceiveDamage(ZombieController enemy)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        nextDamageTime = Time.time + damageCooldown;
        if (hurtSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.96f, 1.04f);
            audioSource.PlayOneShot(hurtSound);
        }

        if (enemy != null)
        {
            enemy.AttackPlayer(transform.position);
            enemy.Knockback(transform.position);
        }

        GameManager.Instance.TakeDamage(1);
    }
}
