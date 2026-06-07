using UnityEngine;

public class Enemy : MonoBehaviour
{

    public float moveSpeed;
    public float waveAmount;
    public float waveSpeed;
    float startY;

    public GameObject bulletPrefab;
    public Transform firePoint;
    float fireRate = 1.5f;
    float nextFire = 0f;

    AudioSource audioSource;
    public AudioClip explosionClip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        waveAmount = Random.Range(0.5f, 1.5f);
        waveSpeed = Random.Range(1f, 3f);
        startY = transform.position.y;
        nextFire = Time.time + Random.Range(0.5f, fireRate);
        audioSource = FindAnyObjectByType<PlayerController>().audioSource;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += moveSpeed * Time.deltaTime * Vector3.right;
        float Y = startY + Mathf.Sin(Time.time * waveSpeed) * waveAmount;
        transform.position = new Vector3(transform.position.x, Y, transform.position.z);
        if (Time.time >= nextFire)
        {
            Shoot();
            nextFire = Time.time + fireRate;
        }
        if (transform.position.x > 4f || transform.position.x < -4f)
        {
            Destroy(gameObject);
        }
    }

    void Shoot()
    {
        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    }

    public void OnHitEnemy()
    {
        audioSource.PlayOneShot(explosionClip);
        Destroy(gameObject);
    }
}
