using UnityEngine;

public class Meteor : MonoBehaviour
{

    public float moveSpeed;
    public float waveAmount;
    public float waveSpeed;
    float startY;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        waveAmount = Random.Range(0.5f, 1f);
        waveSpeed = Random.Range(1f, 1.5f);
        startY = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += moveSpeed * Time.deltaTime * Vector3.right;
        float Y = startY + Mathf.Sin(Time.time * waveSpeed) * waveAmount;
        transform.position = new Vector3(transform.position.x, Y, transform.position.z);
        if (transform.position.x > 4f || transform.position.x < -4f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player hit by meteor!");
            if (other.TryGetComponent<PlayerController>(out var player))
            {
                player.GameOver();
            }
        }
    }
}
