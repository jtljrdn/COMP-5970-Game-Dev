using UnityEngine;

public class Bullet : MonoBehaviour
{

    float speed = 7f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(speed * Time.deltaTime * Vector3.up);
        if (transform.position.y > 10f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.IncreaseScore(10);
            }
            if (other.TryGetComponent<Enemy>(out var enemy))
            {
                enemy.OnHitEnemy();
            }
            Destroy(gameObject);
        }
    }
}
