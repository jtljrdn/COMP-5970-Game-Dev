using UnityEngine;

public class FallingObject : MonoBehaviour
{

    float fallSpeed;

    float destroyY = -6f;
    public float upperRange = 8f;
    public float lowerRange = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fallSpeed = Random.Range(lowerRange, upperRange);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }
}
