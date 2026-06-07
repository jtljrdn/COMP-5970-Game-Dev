using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    float speed = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += speed * Time.deltaTime * Vector3.down;
        if (transform.position.y < -6f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player hit!");
            if (other.TryGetComponent<PlayerController>(out var player))
            {
                player.DecreaseLives();
            }
            Destroy(gameObject);
        }
    }
}
