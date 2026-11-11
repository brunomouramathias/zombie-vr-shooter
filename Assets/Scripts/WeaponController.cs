using System.Collections;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public GameObject spawnPoint;
    public float maxShootDistance = 15f;
    public float fireRate = 1.0f;

    private bool isShooting;
    private AudioSource audioSource;
    private Animation weaponAnimation;

    void Start()
    {
        isShooting = false;
        audioSource = GetComponent<AudioSource>();
        weaponAnimation = GetComponent<Animation>();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            return;
        }

        if (spawnPoint == null) return;

        RaycastHit hit;
        Debug.DrawRay(spawnPoint.transform.position, spawnPoint.transform.forward * maxShootDistance, Color.red);

        if (Physics.Raycast(spawnPoint.transform.position, spawnPoint.transform.forward, out hit, maxShootDistance))
        {
            bool isEnemy = hit.collider.CompareTag("Zombie") ||
                           hit.collider.GetComponentInParent<ZombieController>() != null;

            if (isEnemy && !isShooting)
            {
                StartCoroutine(Shoot());
            }
        }
    }

    private IEnumerator Shoot()
    {
        isShooting = true;

        GameObject bulletPrefab = Resources.Load<GameObject>("Bullet");
        if (bulletPrefab != null && spawnPoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(spawnPoint.transform.forward * 500f);
            }

            // Garante marcador de projétil
            if (bullet.GetComponent<BulletProjectile>() == null)
            {
                bullet.AddComponent<BulletProjectile>();
            }

            Destroy(bullet, 2.5f);
        }

        if (audioSource != null)
        {
            audioSource.Play();
        }

        if (weaponAnimation != null)
        {
            weaponAnimation.Play();
        }

        yield return new WaitForSeconds(fireRate);
        isShooting = false;
    }
}
